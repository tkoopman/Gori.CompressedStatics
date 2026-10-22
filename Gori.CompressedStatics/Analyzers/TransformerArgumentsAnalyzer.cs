using System.Collections.Immutable;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.CompressedStatics.Analyzers;

/// <summary>
/// Validates explicit constructor-argument arrays without resolving runtime argument types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TransformerArgumentsAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.InvalidTransformerArgument];

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
            || !symbol.TryGetAttribute(metadataName, out AttributeData? attribute)
            || !ValueTransformerHelper.IsValidTransformerValue(attribute.GetNamedArgumentAsString(nameof(CompressFileAttribute.Transformer)), out string? type, out string? member)
            || type is null
            || member is not null
            || !attribute.TryGetNamedArgument(nameof(CompressFileAttribute.TransformerArguments), out TypedConstant arguments))
        {
            return;
        }

        if (arguments.Kind != TypedConstantKind.Array || arguments.IsNull)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidTransformerArgument, syntax.GetLocation(), symbol.Name));
        }
    }
}
