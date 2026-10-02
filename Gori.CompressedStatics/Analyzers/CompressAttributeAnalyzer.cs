using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Gori.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.CompressedStatics.Analyzers;

/// <summary>
/// Analyzes the usage of compression attributes in C# code and reports diagnostics for incorrect usage.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CompressAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [
            DiagnosticDescriptors.MemberMustBeStatic,
            DiagnosticDescriptors.MemberMustBePartial,
            DiagnosticDescriptors.GenericTypesUnsupported,
            DiagnosticDescriptors.MethodMustHaveNoParameters,
            DiagnosticDescriptors.IgnoreCacheModeOnMethods,
            DiagnosticDescriptors.MemberMustReturnStringOrByteArray,
            DiagnosticDescriptors.IgnoreAlgorithmOnTarMember,
            DiagnosticDescriptors.TypeMustBePartial,
            DiagnosticDescriptors.MemberMustBePartialWhenTarHasStaticConstructor,
            DiagnosticDescriptors.OneAttributePerMember,

            // These only raised by the generator. Added here so still show up in IDE.
            DiagnosticDescriptors.BrotliNotAvailable,
            DiagnosticDescriptors.InvalidBase64String,
        ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsAttribute(out AttributeSyntax? attributeSyntax, out ISymbol? memberSymbol, out string? attributeMetadataName))
        {
            return;
        }

        CompressAttributeType attributeType = AttributeHelper.GetCompressAttributeType(attributeMetadataName);

        if (attributeType is CompressAttributeType.Unknown)
        {
            return;
        }

        if (memberSymbol.IsWithinGenericType())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.GenericTypesUnsupported,
                    attributeSyntax.GetLocation(),
                    attributeType.GetShortName()));
        }

        bool isPartial = memberSymbol.IsPartial(context.CancellationToken);
        _ = attributeSyntax.TryGetNamedArgument(nameof(CompressFileAttribute.CacheMode), out AttributeArgumentSyntax? cacheModeArgument);

        switch (attributeType)
        {
            case CompressAttributeType.TarCompress:
                if (!isPartial)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.TypeMustBePartial,
                            attributeSyntax.GetLocation(),
                            memberSymbol.Name,
                            attributeType.GetShortName()));
                }

                break;

            case CompressAttributeType.CompressString:
                if (memberSymbol.TryGetAttribute(Const.CompressFileAttributeFullName, out _))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.OneAttributePerMember,
                            attributeSyntax.GetLocation(),
                            memberSymbol.Name,
                            Const.CompressFileAttributeShortName,
                            Const.CompressStringAttributeShortName));
                }

                goto case CompressAttributeType.CompressFile;

            case CompressAttributeType.CompressFile:
                if (!memberSymbol.IsStatic)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.MemberMustBeStatic,
                            attributeSyntax.GetLocation(),
                            memberSymbol.Name,
                            attributeType.GetShortName()));
                }

                switch (memberSymbol)
                {
                    case IMethodSymbol methodSymbol:
                        if (!isPartial)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    DiagnosticDescriptors.MemberMustBePartial,
                                    attributeSyntax.GetLocation(),
                                    memberSymbol.Name,
                                    attributeType.GetShortName()));
                        }

                        if (!AttributeHelper.IsValidReturnType(methodSymbol.ReturnType))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    DiagnosticDescriptors.MemberMustReturnStringOrByteArray,
                                    attributeSyntax.GetLocation(),
                                    memberSymbol.Name,
                                    attributeType.GetShortName()));
                        }

                        if (cacheModeArgument is not null)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    DiagnosticDescriptors.IgnoreCacheModeOnMethods,
                                    cacheModeArgument.GetLocation(),
                                    memberSymbol.Name));
                        }

                        if (methodSymbol.Parameters.Length > 0)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    DiagnosticDescriptors.MethodMustHaveNoParameters,
                                    attributeSyntax.GetLocation(),
                                    memberSymbol.Name,
                                    attributeType.GetShortName()));
                        }

                        break;

                    case IPropertySymbol propertySymbol:
                        if (!AttributeHelper.IsValidReturnType(propertySymbol.Type))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    DiagnosticDescriptors.MemberMustReturnStringOrByteArray,
                                    attributeSyntax.GetLocation(),
                                    memberSymbol.Name,
                                    attributeType.GetShortName()));
                        }

                        bool hasStaticConstructor = propertySymbol.ContainingType
                                                                  .GetMembers()
                                                                  .OfType<IMethodSymbol>()
                                                                  .Any(member => member.MethodKind == MethodKind.StaticConstructor
                                                                              && !IsGeneratedConstructor(member, context.CancellationToken));

                        bool hasDefaultCacheMode = cacheModeArgument is null
                            || cacheModeArgument.Expression.ToString().EndsWith("CompressionCacheMode.OnInit", StringComparison.Ordinal);
                        bool isTarCompressedProperty =  hasDefaultCacheMode && IsTarCompressedProperty(propertySymbol);

                        if (isTarCompressedProperty)
                        {
                            if (attributeSyntax.TryGetNamedArgument(nameof(CompressFileAttribute.Algorithm), out AttributeArgumentSyntax? algorithmArgument))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        DiagnosticDescriptors.IgnoreAlgorithmOnTarMember,
                                        algorithmArgument.GetLocation(),
                                        memberSymbol.Name));
                            }

                            if (!isPartial && hasStaticConstructor)
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        DiagnosticDescriptors.MemberMustBePartialWhenTarHasStaticConstructor,
                                        attributeSyntax.GetLocation(),
                                        memberSymbol.Name,
                                        memberSymbol.ContainingType.Name));
                            }
                        }

                        if (!isPartial && !isTarCompressedProperty)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    DiagnosticDescriptors.MemberMustBePartial,
                                    attributeSyntax.GetLocation(),
                                    memberSymbol.Name,
                                    attributeType.GetShortName()));
                        }

                        break;

                    default:
                        // This case should not occur, but we include it for completeness.
                        // AttributeUsage should handle this.
                        break;
                }

                break;

            case CompressAttributeType.Unknown:
            default:
                break;
        }
    }

    /// <summary>
    /// Checks if the property's containing type has a TarCompress attribute and if the property is not excluded from compression.
    /// </summary>
    /// <remarks>
    /// This does not handle the case where property is excluded due to not having the default cache mode.
    /// That should be handled outside of this call.
    /// </remarks>
    /// <param name="propertySymbol">The property symbol to check.</param>
    /// <returns>True if the property is tar compressed; otherwise, false.</returns>
    private static bool IsTarCompressedProperty(IPropertySymbol propertySymbol)
    {
        if (!TryGetTarAttribute(propertySymbol.ContainingType, out AttributeData? tarAttribute))
        {
            return false;
        }

        ImmutableHashSet<string> excludedPropertyNames = GetExcludedPropertyNames(tarAttribute);
        return !excludedPropertyNames.Contains(propertySymbol.Name);
    }

    private static bool TryGetTarAttribute(INamedTypeSymbol typeSymbol, [NotNullWhen(true)] out AttributeData? tarAttribute)
    {
        foreach (AttributeData attributeData in typeSymbol.GetAttributes())
        {
            if (!AttributeHelper.IsTarCompressAttribute(attributeData.AttributeClass?.ToDisplayString()))
            {
                continue;
            }

            tarAttribute = attributeData;
            return true;
        }

        tarAttribute = null;
        return false;
    }

    private static ImmutableHashSet<string> GetExcludedPropertyNames(AttributeData tarAttribute)
    {
        foreach (KeyValuePair<string, TypedConstant> namedArgument in tarAttribute.NamedArguments)
        {
            if (!StringComparer.Ordinal.Equals(namedArgument.Key, nameof(TarCompressAttribute.Exclude))
                || namedArgument.Value.IsNull
                || namedArgument.Value.Kind != TypedConstantKind.Array)
            {
                continue;
            }

            ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder(StringComparer.Ordinal);
            foreach (TypedConstant value in namedArgument.Value.Values)
            {
                if (value.Value is string propertyName
                    && !propertyName.IsWhiteSpace())
                {
                    _ = builder.Add(propertyName);
                }
            }

            return builder.ToImmutable();
        }

        return [];
    }

    /// <summary>
    /// Checks if a static constructor was generated by the source generator by examining its syntax for the auto-generated marker.
    /// </summary>
    /// <param name="methodSymbol">The static constructor method symbol to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the constructor was generated; otherwise, false.</returns>
    private static bool IsGeneratedConstructor(IMethodSymbol methodSymbol, CancellationToken cancellationToken)
    {
        if (methodSymbol.MethodKind != MethodKind.StaticConstructor || methodSymbol.DeclaringSyntaxReferences.IsEmpty)
        {
            return false;
        }

        try
        {
            SyntaxNode syntaxNode = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);

            // Check if the containing syntax node (the file or class) has an auto-generated comment
            SyntaxNode root = syntaxNode.SyntaxTree.GetRoot(cancellationToken);
            SyntaxTriviaList leadingTrivia = root.GetLeadingTrivia();

            foreach (SyntaxTrivia trivia in leadingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    string triviaText = trivia.ToString();
                    if (triviaText.Contains("auto-generated", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // If we can't determine due to cancellation, assume it's user-generated (safer for warnings)
            return false;
        }

        return false;
    }
}
