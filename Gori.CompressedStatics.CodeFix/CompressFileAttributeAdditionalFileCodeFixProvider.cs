using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Gori.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Gori.CompressedStatics.CodeFix;

/// <summary>
/// A code fix provider that updates <see cref="CompressFileAttribute.RelativeToProject"/>
/// when the opposite path resolves to an additional file.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CompressFileAttributeAdditionalFileCodeFixProvider))]
public sealed class CompressFileAttributeAdditionalFileCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds
        => [DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        Diagnostic diagnostic = context.Diagnostics[0];
        if (root is null || !diagnostic.Properties.TryGetValue("RelativeToProjectAction", out string? action))
        {
            return;
        }

        bool currentRelativeToProject = StringComparer.Ordinal.Equals(action, "Remove");
        if (!currentRelativeToProject && !StringComparer.Ordinal.Equals(action, "Add"))
        {
            return;
        }

        SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        MemberDeclarationSyntax? declaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (declaration is null)
        {
            return;
        }

        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null
            || !TryGetCompressFileAttribute(declaration, semanticModel, out AttributeSyntax? attribute, out _, out bool attributeRelativeToProject)
            || attributeRelativeToProject != currentRelativeToProject)
        {
            return;
        }

        string toggleTitle = currentRelativeToProject
                ? "Remove RelativeToProject"
                : "Set RelativeToProject = true";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: toggleTitle,
                createChangedDocument: cancellationToken => UpdateRelativeToProjectAsync(context.Document, attribute, currentRelativeToProject, cancellationToken),
                equivalenceKey: "CompressFileAttribute.ToggleRelativeToProject"),
            diagnostic);

        if (diagnostic.Properties.TryGetValue("RelativeFilePath", out string? rewrittenFilePath)
            && rewrittenFilePath is not null
            && !rewrittenFilePath.IsWhiteSpace())
        {
            string rewriteTitle = currentRelativeToProject
                ? "Rewrite file path for RelativeToProject = true"
                : "Rewrite file path for RelativeToProject = false";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: rewriteTitle,
                    createChangedDocument: cancellationToken => UpdateFilePathAsync(context.Document, attribute, rewrittenFilePath, cancellationToken),
                    equivalenceKey: "CompressFileAttribute.RewriteFilePath"),
                diagnostic);
        }
    }

    private static async Task<Document> UpdateRelativeToProjectAsync(
        Document document,
        AttributeSyntax attribute,
        bool relativeToProject,
        CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        if (relativeToProject)
        {
            if (attribute.ArgumentList is null
                || !attribute.TryGetNamedArgument(nameof(CompressFileAttribute.RelativeToProject), out AttributeArgumentSyntax? removeNamedArgument))
            {
                return document;
            }

            SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attribute.ArgumentList.Arguments.Remove(removeNamedArgument);
            AttributeSyntax updatedAttribute = attribute.WithArgumentList(attribute.ArgumentList.WithArguments(arguments))
                                                        .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = root.ReplaceNode(attribute, updatedAttribute);
            return document.WithSyntaxRoot(newRoot);
        }

        AttributeArgumentSyntax newNamedArgument = SyntaxFactory.AttributeArgument(
            nameEquals: SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(nameof(CompressFileAttribute.RelativeToProject))),
            nameColon: null,
            expression: SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));

        AttributeSyntax newAttributeSyntax;
        if (attribute.TryGetNamedArgument(nameof(CompressFileAttribute.RelativeToProject), out AttributeArgumentSyntax? existingNamedArgument))
        {
            newAttributeSyntax = (AttributeSyntax)attribute.ReplaceNode(existingNamedArgument, newNamedArgument)
                                                           .WithAdditionalAnnotations(Formatter.Annotation);
        }
        else if (attribute.ArgumentList is null)
        {
            return document;
        }
        else
        {
            SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attribute.ArgumentList.Arguments.Add(newNamedArgument);
            newAttributeSyntax = attribute.WithArgumentList(attribute.ArgumentList.WithArguments(arguments))
                                          .WithAdditionalAnnotations(Formatter.Annotation);
        }

        SyntaxNode updatedRoot = root.ReplaceNode(attribute, newAttributeSyntax);
        return document.WithSyntaxRoot(updatedRoot);
    }

    private static async Task<Document> UpdateFilePathAsync(
        Document document,
        AttributeSyntax attribute,
        string filePath,
        CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || attribute.ArgumentList is null || attribute.ArgumentList.Arguments.Count < 1)
        {
            return document;
        }

        SyntaxNode filePathExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(filePath));
        AttributeArgumentSyntax updatedFilePathArgument = attribute.ArgumentList
                                                                   .Arguments[0]
                                                                   .WithExpression((ExpressionSyntax)filePathExpression);

        SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attribute.ArgumentList
                                                                          .Arguments
                                                                          .Replace(attribute.ArgumentList.Arguments[0], updatedFilePathArgument);

        AttributeSyntax updatedAttribute = attribute.WithArgumentList(attribute.ArgumentList.WithArguments(arguments))
                                                    .WithAdditionalAnnotations(Formatter.Annotation);

        SyntaxNode newRoot = root.ReplaceNode(attribute, updatedAttribute);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool TryGetCompressFileAttribute(
        MemberDeclarationSyntax declaration,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out AttributeSyntax? attribute,
        out string filePath,
        out bool relativeToProject)
    {
        foreach (AttributeListSyntax attributeList in declaration.AttributeLists)
        {
            foreach (AttributeSyntax currentAttribute in attributeList.Attributes)
            {
                if (!AttributeHelper.IsCompressFileAttribute(currentAttribute.Name.ToString())
                    || currentAttribute.ArgumentList is null
                    || currentAttribute.ArgumentList.Arguments.Count < 1
                    || !semanticModel.GetConstantValue(currentAttribute.ArgumentList.Arguments[0].Expression).TryGetValueAsString(out filePath)
                    || string.IsNullOrWhiteSpace(filePath))
                {
                    continue;
                }

                relativeToProject = false;
                if (currentAttribute.TryGetNamedArgument(nameof(CompressFileAttribute.RelativeToProject), out AttributeArgumentSyntax? namedArgument)
                    && semanticModel.GetConstantValue(namedArgument.Expression).TryGetValueAsBool(out bool namedRelativeToProject))
                {
                    relativeToProject = namedRelativeToProject;
                }

                attribute = currentAttribute;
                return true;
            }
        }

        attribute = null;
        filePath = string.Empty;
        relativeToProject = false;
        return false;
    }
}
