using System.Collections.Immutable;
using System.Composition;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Gori.CompressedStatics.CodeFix;

/// <summary>
/// Resolves incompatible transformer argument and member selections.
/// </summary>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TransformerArgumentsCodeFixProvider))]
public sealed class TransformerArgumentsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds
        => [DiagnosticDescriptors.TransformerArgumentsWithMember.Id, DiagnosticDescriptors.TransformerArgumentsRequireType.Id];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics[0];
        AttributeSyntax? attribute = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
            .FirstAncestorOrSelf<AttributeSyntax>();
        if (attribute is null
            || !attribute.TryGetNamedArgument(nameof(CompressFileAttribute.TransformerArguments), out AttributeArgumentSyntax? arguments))
        {
            return;
        }

        AttributeSyntax? withoutArguments = attribute.RemoveNode(arguments, SyntaxRemoveOptions.KeepExteriorTrivia);
        if (withoutArguments is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Remove TransformerArguments",
                    createChangedDocument: cancellationToken => ReplaceAttributeAsync(context.Document, attribute, withoutArguments, cancellationToken),
                    equivalenceKey: "TransformerArguments.RemoveArguments"),
                diagnostic);
        }

        if (diagnostic.Id != DiagnosticDescriptors.TransformerArgumentsWithMember.Id
            || !attribute.TryGetNamedArgument(nameof(CompressFileAttribute.Transformer), out AttributeArgumentSyntax? transformer))
        {
            return;
        }

        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetConstantValue(transformer.Expression, context.CancellationToken).Value is not string selector)
        {
            return;
        }

        int separator = selector.IndexOf("::", StringComparison.Ordinal);
        if (separator <= 0)
        {
            return;
        }

        ExpressionSyntax typeName = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(selector[..separator].Trim())).WithTriviaFrom(transformer.Expression);
        AttributeSyntax withoutMember = attribute.ReplaceNode(transformer.Expression, typeName);
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove transformer member selector",
                createChangedDocument: cancellationToken => ReplaceAttributeAsync(context.Document, attribute, withoutMember, cancellationToken),
                equivalenceKey: "TransformerArguments.RemoveMember"),
            diagnostic);
    }

    private static async Task<Document> ReplaceAttributeAsync(Document document, AttributeSyntax attribute, AttributeSyntax replacement, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return root is null
            ? document
            : document.WithSyntaxRoot(root.ReplaceNode(attribute, replacement.WithAdditionalAnnotations(Formatter.Annotation)));
    }
}
