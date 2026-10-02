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
/// A code fix provider that adds the 'partial' modifier to member and type declarations when required by the diagnostics.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPartialModifierCodeFixProvider))]
public sealed class AddPartialModifierCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds
        => [
            DiagnosticDescriptors.MemberMustBePartial.Id,
            DiagnosticDescriptors.TypeMustBePartial.Id,
            DiagnosticDescriptors.MemberMustBePartialWhenTarHasStaticConstructor.Id,
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

        // Find the starting node: either a member or type declaration
        MemberDeclarationSyntax? memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        TypeDeclarationSyntax? typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

        if (memberDeclaration is null && typeDeclaration is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add 'partial' modifier",
                createChangedDocument: cancellationToken => AddPartialModifiersAsync(context.Document, memberDeclaration, typeDeclaration, cancellationToken),
                equivalenceKey: "AddPartialModifier"),
            diagnostic);
    }

    private static async Task<Document> AddPartialModifiersAsync(Document document, MemberDeclarationSyntax? memberDeclaration, TypeDeclarationSyntax? typeDeclaration, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        SyntaxNode newRoot = root;

        // If we have a member declaration, make it partial if it isn't already
        if (memberDeclaration is not null)
        {
            SyntaxTokenList modifiers = memberDeclaration.Modifiers;
            if (!modifiers.Any(SyntaxKind.PartialKeyword))
            {
                SyntaxTokenList newModifiers = modifiers.InsertModifier(SyntaxKind.PartialKeyword);
                MemberDeclarationSyntax newMemberDeclaration = memberDeclaration.WithModifiers(newModifiers)
                                                                                .WithAdditionalAnnotations(Formatter.Annotation);
                newRoot = newRoot.ReplaceNode(memberDeclaration, newMemberDeclaration);

                // Update memberDeclaration reference to the new node in newRoot
                memberDeclaration = newRoot.FindNode(memberDeclaration.Span) as MemberDeclarationSyntax;
            }

            // If we started from a member, use it as the entry point for walking up the type hierarchy
            typeDeclaration = memberDeclaration!.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        }

        // Walk up and ensure all containing types are partial (for nested types)
        SyntaxNode? currentNode = typeDeclaration;
        while ((currentNode = (currentNode as TypeDeclarationSyntax) ?? currentNode?.FirstAncestorOrSelf<TypeDeclarationSyntax>()) is not null)
        {
            var typeToUpdate = (TypeDeclarationSyntax)currentNode;
            SyntaxTokenList modifiers = typeToUpdate.Modifiers;

            if (!modifiers.Any(SyntaxKind.PartialKeyword))
            {
                SyntaxTokenList newModifiers = modifiers.InsertModifier(SyntaxKind.PartialKeyword);
                TypeDeclarationSyntax newType = typeToUpdate.WithModifiers(newModifiers)
                    .WithAdditionalAnnotations(Formatter.Annotation);
                newRoot = newRoot.ReplaceNode(typeToUpdate, newType);

                // Re-find the updated type in newRoot to get correct parent reference
                typeToUpdate = newRoot.FindNode(typeToUpdate.Span) as TypeDeclarationSyntax;
            }

            // Get the parent from the updated tree
            currentNode = typeToUpdate?.Parent;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
