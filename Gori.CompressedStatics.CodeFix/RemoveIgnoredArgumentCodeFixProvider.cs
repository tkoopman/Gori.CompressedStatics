using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Gori.CompressedStatics.CodeFix;

/// <summary>
/// A code fix provider that removes ignored arguments from attribute declarations when required by the diagnostics.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveIgnoredArgumentCodeFixProvider))]
public sealed class RemoveIgnoredArgumentCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds
        => [
            DiagnosticDescriptors.IgnoreCacheModeOnMethods.Id,
            DiagnosticDescriptors.IgnoreAlgorithmOnTarMember.Id,
        ];

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
        SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        AttributeArgumentSyntax? argument = node.FirstAncestorOrSelf<AttributeArgumentSyntax>();
        if (argument is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove ignored argument",
                createChangedDocument: cancellationToken => RemoveArgumentAsync(context.Document, argument, cancellationToken),
                equivalenceKey: "RemoveIgnoredArgument"),
            diagnostic);
    }

    private static async Task<Document> RemoveArgumentAsync(Document document, AttributeArgumentSyntax argument, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        if (argument.Parent is not AttributeArgumentListSyntax argumentList)
        {
            return document;
        }

        SeparatedSyntaxList<AttributeArgumentSyntax> newArguments = argumentList.Arguments.Remove(argument);
        AttributeArgumentListSyntax newArgumentList = argumentList.WithArguments(newArguments);

        if (argumentList.Parent is not AttributeSyntax attribute)
        {
            return document;
        }

        AttributeSyntax newAttribute = attribute.WithArgumentList(newArgumentList).WithAdditionalAnnotations(Formatter.Annotation);
        SyntaxNode newRoot = root.ReplaceNode(attribute, newAttribute);
        return document.WithSyntaxRoot(newRoot);
    }
}
