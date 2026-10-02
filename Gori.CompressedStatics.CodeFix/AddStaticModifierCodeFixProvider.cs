using System.Collections.Immutable;

using Gori.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Gori.CompressedStatics.CodeFix;

/// <summary>
/// A code fix provider that adds the 'static' modifier to member declarations when required by the diagnostics.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddStaticModifierCodeFixProvider))]
public sealed class AddStaticModifierCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds
        => [DiagnosticDescriptors.MemberMustBeStatic.Id];

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

        // Navigate to the method or property declaration
        MemberDeclarationSyntax? memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDeclaration is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add 'static' modifier",
                createChangedDocument: cancellationToken => AddStaticModifierAsync(context.Document, memberDeclaration, cancellationToken),
                equivalenceKey: "AddStaticModifier"),
            diagnostic);
    }

    private static async Task<Document> AddStaticModifierAsync(Document document, MemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // Check if the member already has the static modifier
        SyntaxTokenList modifiers = memberDeclaration.Modifiers;
        if (modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return document;
        }

        // Insert static modifier in the correct position
        SyntaxTokenList newModifiers = modifiers.InsertModifier(SyntaxKind.StaticKeyword);
        MemberDeclarationSyntax newMemberDeclaration = memberDeclaration.WithModifiers(newModifiers)
                                                                        .WithAdditionalAnnotations(Formatter.Annotation);

        SyntaxNode newRoot = root.ReplaceNode(memberDeclaration, newMemberDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}
