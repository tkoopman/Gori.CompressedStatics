using System.Collections.Immutable;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.CompressedStatics.Analyzers;

/// <summary>
/// Validates transformer selector syntax without loading transformer instances.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TransformerSelectorAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.InvalidTransformerFormat];

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

        if (!ValueTransformerHelper.IsValidTransformerValue(attribute.GetNamedArgumentAsString(nameof(CompressFileAttribute.Transformer)), out _, out _))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidTransformerFormat, syntax.GetLocation(), symbol.Name));
        }
    }
}
