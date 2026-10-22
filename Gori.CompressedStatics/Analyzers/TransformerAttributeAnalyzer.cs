using System.Collections.Immutable;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.CompressedStatics.Analyzers;

/// <summary>
/// Validates transformer argument configuration without loading transformer instances.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TransformerAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.TransformerArgumentsWithMember, DiagnosticDescriptors.TransformerArgumentsRequireType];

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
            || !attribute.TryGetNamedArgument(nameof(CompressFileAttribute.TransformerArguments), out TypedConstant _))
        {
            return;
        }

        string? selector = attribute.GetNamedArgumentAsString(nameof(CompressFileAttribute.Transformer));
        if (!ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member))
        {
            return;
        }

        DiagnosticDescriptor? descriptor = type is null
            ? DiagnosticDescriptors.TransformerArgumentsRequireType
            : member is not null ? DiagnosticDescriptors.TransformerArgumentsWithMember : null;
        if (descriptor is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation(), symbol.Name));
        }
    }
}
