using System.Collections.Immutable;

using Gori.Common.Roslyn;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using static Gori.CompressedStatics.ValueTransformerHelper;

namespace Gori.CompressedStatics.Analyzers;

/// <summary>
/// Validates explicit constructor-argument arrays without resolving runtime argument types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TransformerAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [
            DiagnosticDescriptors.TransformerArgumentsRequireType,
            DiagnosticDescriptors.TransformerArgumentsWithMember,
            DiagnosticDescriptors.TransformerConstructorNotFound,
            DiagnosticDescriptors.TransformerConstructorIsAmbiguous,
            DiagnosticDescriptors.InvalidTransformerArgument,
            DiagnosticDescriptors.InvalidTransformerFormat,
            DiagnosticDescriptors.TransformerTypeNotFound,
            DiagnosticDescriptors.TransformerTypeNotConcreteClass,
            DiagnosticDescriptors.TransformerIsAmbiguous,
            DiagnosticDescriptors.TransformerTypeIsGeneric,
            DiagnosticDescriptors.TransformerTypeNotAssignable,
            DiagnosticDescriptors.TransformerTypeHasNoPublicParameterlessConstructor,
            DiagnosticDescriptors.TransformerMemberNotFound,
            DiagnosticDescriptors.TransformerMemberNotAssignable,
            DiagnosticDescriptors.TransformerExceptionThrownLoadingInstance,
            DiagnosticDescriptors.TransformerUnknownErrorLoadingInstance,

            // These only raised by the generator. Added here so still show up in IDE.
            DiagnosticDescriptors.InvalidCSharpTransformerSource,
            DiagnosticDescriptors.TransformerExceptionThrown,
            DiagnosticDescriptors.CSharpRewriterInvalidConfiguration,
            DiagnosticDescriptors.CSharpRewriterConflictingConfiguration,
            DiagnosticDescriptors.CSharpRewriterBindingIssue,
            DiagnosticDescriptors.CSharpRewriterUnresolvable,
            DiagnosticDescriptors.CSharpRewriterMappingIgnoreOverlap,
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
        if (!context.IsAttribute(out AttributeSyntax? syntax, out ISymbol? symbol, out string? metadataName)
            || AttributeHelper.GetCompressAttributeType(metadataName) is not (CompressAttributeType.CompressFile or CompressAttributeType.CompressString)
            || !symbol.TryGetAttribute(metadataName, out AttributeData? attribute))
        {
            return;
        }

        ReturnType returnType = SymbolExtraction.GetReturnType(symbol);

        // No need to report as that is handled already over in CompressAttributeAnalyzer.
        if (returnType is ReturnType.Unsupported)
        {
            return;
        }

        TransformerType transformerType = IsValidTransformerValue(attribute.GetNamedArgumentAsString(nameof(CompressFileAttribute.Transformer)), returnType, out _, out string? member, out ValueTransformerHelper.Error error);
        bool hasArguments = attribute.TryGetNamedArgument(nameof(CompressFileAttribute.TransformerArguments), out TypedConstant arguments);

        if (transformerType is TransformerType.None)
        {
            if (hasArguments && !arguments.IsNull)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TransformerArgumentsRequireType, syntax.GetLocation(), symbol.Name));
            }

            return;
        }

        if (transformerType is TransformerType.NotFound)
        {
            // While not all of these raisable here, and only handled in generator. All have been included for completeness.
            // They already had to be added to SupportedDiagnostics, so they show up in the IDE, so why not here too.
            context.ReportDiagnostic(Diagnostic.Create(
                error switch
                {
                    Error.ConstructorNotFound => DiagnosticDescriptors.TransformerConstructorNotFound,
                    Error.ConstructorIsAmbiguous => DiagnosticDescriptors.TransformerConstructorIsAmbiguous,
                    Error.InvalidArgument => DiagnosticDescriptors.InvalidTransformerArgument,
                    Error.InvalidFormat => DiagnosticDescriptors.InvalidTransformerFormat,
                    Error.TypeNotFound => DiagnosticDescriptors.TransformerTypeNotFound,
                    Error.TypeNotConcreteClass => DiagnosticDescriptors.TransformerTypeNotConcreteClass,
                    Error.IsAmbiguous => DiagnosticDescriptors.TransformerIsAmbiguous,
                    Error.TypeIsGeneric => DiagnosticDescriptors.TransformerTypeIsGeneric,
                    Error.TypeNotAssignable => DiagnosticDescriptors.TransformerTypeNotAssignable,
                    Error.TypeHasNoPublicParameterlessConstructor => DiagnosticDescriptors.TransformerTypeHasNoPublicParameterlessConstructor,
                    Error.MemberNotFound => DiagnosticDescriptors.TransformerMemberNotFound,
                    Error.MemberNotAssignable => DiagnosticDescriptors.TransformerMemberNotAssignable,
                    Error.ExceptionThrown => DiagnosticDescriptors.TransformerExceptionThrownLoadingInstance,
                    _ => DiagnosticDescriptors.TransformerUnknownErrorLoadingInstance,
                },
                syntax.GetLocation(),
                symbol.Name));
            return;
        }

        if (member is not null)
        {
            if (hasArguments && !arguments.IsNull)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TransformerArgumentsWithMember, syntax.GetLocation(), symbol.Name));
            }

            return;
        }

        if (!arguments.IsNull && arguments.Kind is not TypedConstantKind.Array)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidTransformerArgument, syntax.GetLocation(), symbol.Name));
        }
    }
}
