#pragma warning disable IDE0130

using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.Roslyn;

/// <summary>
/// Provides extension methods for <see cref="SyntaxNode"/> and <see cref="SyntaxNodeAnalysisContext"/> to facilitate analysis.
/// </summary>
internal static class SyntaxNodeExtensions
{
    /// <summary>
    /// Determines if the given <paramref name="context"/> is for an attribute node and retrieves its metadata name.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="attributeSyntax">The attribute syntax if found.</param>
    /// <param name="memberSyntax">The member declaration to which the attribute is applied.</param>
    /// <param name="memberSymbol">The symbol of the member to which the attribute is applied.</param>
    /// <param name="attributeMetadataName">The attribute metadata name if found.</param>
    /// <returns>True if the context is for an attribute node; otherwise, false.</returns>
    public static bool IsAttribute(
        this SyntaxNodeAnalysisContext context,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberSyntax,
        [NotNullWhen(true)] out ISymbol? memberSymbol,
        [NotNullWhen(true)] out string? attributeMetadataName)
        => context.Node.IsAttribute(context.SemanticModel, out attributeSyntax, out memberSyntax, out memberSymbol, out attributeMetadataName, context.CancellationToken);

    /// <inheritdoc cref="IsAttribute(SyntaxNodeAnalysisContext, out AttributeSyntax?, out MemberDeclarationSyntax?, out ISymbol?, out string?)"/>
    public static bool IsAttribute(
        this SyntaxNodeAnalysisContext context,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberSyntax,
        [NotNullWhen(true)] out string? attributeMetadataName)
        => context.Node.IsAttribute(context.SemanticModel, out attributeSyntax, out memberSyntax, out attributeMetadataName, context.CancellationToken);

    /// <inheritdoc cref="IsAttribute(SyntaxNodeAnalysisContext, out AttributeSyntax?, out MemberDeclarationSyntax?, out ISymbol?, out string?)"/>
    public static bool IsAttribute(
        this SyntaxNodeAnalysisContext context,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out ISymbol? memberSymbol,
        [NotNullWhen(true)] out string? attributeMetadataName)
        => context.Node.IsAttribute(context.SemanticModel, out attributeSyntax, out _, out memberSymbol, out attributeMetadataName, context.CancellationToken);

    /// <summary>
    /// Determines if the given <paramref name="context"/> is for an attribute node with the specified metadata name.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="attributeMetadataName">The attribute metadata name to check for.</param>
    /// <param name="attributeSyntax">The attribute syntax if found.</param>
    /// <param name="memberSyntax">The member declaration to which the attribute is applied.</param>
    /// <param name="memberSymbol">The symbol of the member to which the attribute is applied.</param>
    /// <returns>True if the context is for an attribute node with the specified metadata name; otherwise, false.</returns>
    public static bool IsAttribute(
        this SyntaxNodeAnalysisContext context,
        string attributeMetadataName,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberSyntax,
        [NotNullWhen(true)] out ISymbol? memberSymbol)
        => context.Node.IsAttribute(context.SemanticModel, attributeMetadataName, out attributeSyntax, out memberSyntax, out memberSymbol, context.CancellationToken);

    /// <inheritdoc cref="IsAttribute(SyntaxNodeAnalysisContext,string,out AttributeSyntax,out MemberDeclarationSyntax,out ISymbol)"/>
    public static bool IsAttribute(
        this SyntaxNodeAnalysisContext context,
        string attributeMetadataName,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberSyntax)
        => context.Node.IsAttribute(context.SemanticModel, attributeMetadataName, out attributeSyntax, out memberSyntax, context.CancellationToken);

    /// <inheritdoc cref="IsAttribute(SyntaxNodeAnalysisContext,string,out AttributeSyntax,out MemberDeclarationSyntax,out ISymbol)"/>
    public static bool IsAttribute(
        this SyntaxNodeAnalysisContext context,
        string attributeMetadataName,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out ISymbol? memberSymbol)
        => context.Node.IsAttribute(context.SemanticModel, attributeMetadataName, out attributeSyntax, out _, out memberSymbol, context.CancellationToken);

    /// <summary>
    /// Determines if the given <paramref name="syntaxNode"/> is an attribute and retrieves its metadata name.
    /// </summary>
    /// <param name="syntaxNode">The syntax node to check.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="attributeSyntax">The attribute syntax if found.</param>
    /// <param name="memberSyntax">The member declaration to which the attribute is applied.</param>
    /// <param name="memberSymbol">The symbol of the member to which the attribute is applied.</param>
    /// <param name="attributeMetadataName">The attribute metadata name if found.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the syntax node is an attribute; otherwise, false.</returns>
    public static bool IsAttribute(
        this SyntaxNode syntaxNode,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberSyntax,
        [NotNullWhen(true)] out ISymbol? memberSymbol,
        [NotNullWhen(true)] out string? attributeMetadataName,
        CancellationToken cancellationToken)
    {
        if (!syntaxNode.IsAttribute(semanticModel, out attributeSyntax, out memberSyntax, out attributeMetadataName, cancellationToken))
        {
            memberSymbol = null;
            return false;
        }

        memberSymbol = semanticModel.GetDeclaredSymbol(memberSyntax, cancellationToken);
        return memberSymbol is not null && attributeMetadataName is not null;
    }

    /// <inheritdoc cref="IsAttribute(SyntaxNode, SemanticModel, out AttributeSyntax, out MemberDeclarationSyntax, out ISymbol, out string, CancellationToken)"/>
    public static bool IsAttribute(
        this SyntaxNode syntaxNode,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberSyntax,
        [NotNullWhen(true)] out string? attributeMetadataName,
        CancellationToken cancellationToken)
    {
        if (syntaxNode is not AttributeSyntax attribute)
        {
            attributeSyntax = null;
            attributeMetadataName = null;
            memberSyntax = null;
            return false;
        }

        attributeSyntax = attribute;

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax, cancellationToken);
        var attributeSymbol = symbolInfo.Symbol as IMethodSymbol;
        attributeMetadataName = attributeSymbol?.ContainingType?.ToDisplayString();

        memberSyntax = attributeSyntax.Parent?.Parent as MemberDeclarationSyntax;
        return memberSyntax is not null && attributeMetadataName is not null;
    }

    /// <inheritdoc cref="IsAttribute(SyntaxNode, SemanticModel, out AttributeSyntax, out MemberDeclarationSyntax, out ISymbol, out string, CancellationToken)"/>
    public static bool IsAttribute(
        this SyntaxNode syntaxNode,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out ISymbol? memberSymbol,
        [NotNullWhen(true)] out string? attributeMetadataName,
        CancellationToken cancellationToken)
        => syntaxNode.IsAttribute(semanticModel, out attributeSyntax, out _, out memberSymbol, out attributeMetadataName, cancellationToken);

    /// <summary>
    /// Determines if the given <paramref name="syntaxNode"/> is an attribute with the specified metadata name.
    /// </summary>
    /// <param name="syntaxNode">The syntax node to check.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="attributeMetadataName">The attribute metadata name to check for.</param>
    /// <param name="attributeSyntax">The attribute syntax if found.</param>
    /// <param name="memberSyntax">The member declaration to which the attribute is applied.</param>
    /// <param name="memberSymbol">The symbol of the member to which the attribute is applied.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the syntax node is an attribute with the specified metadata name; otherwise, false.</returns>
    public static bool IsAttribute(
        this SyntaxNode syntaxNode,
        SemanticModel semanticModel,
        string attributeMetadataName,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberSyntax,
        [NotNullWhen(true)] out ISymbol? memberSymbol,
        CancellationToken cancellationToken)
        => syntaxNode.IsAttribute(semanticModel, out attributeSyntax, out memberSyntax, out memberSymbol, out string? foundAttributeMetadataName, cancellationToken)
        && foundAttributeMetadataName.Equals(attributeMetadataName, StringComparison.Ordinal);

    /// <inheritdoc cref="IsAttribute(SyntaxNode, SemanticModel, string, out AttributeSyntax, out MemberDeclarationSyntax, out ISymbol, CancellationToken)"/>
    public static bool IsAttribute(
        this SyntaxNode syntaxNode,
        SemanticModel semanticModel,
        string attributeMetadataName,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out MemberDeclarationSyntax? memberSyntax,
        CancellationToken cancellationToken)
        => syntaxNode.IsAttribute(semanticModel, out attributeSyntax, out memberSyntax, out string? foundAttributeMetadataName, cancellationToken)
        && foundAttributeMetadataName.Equals(attributeMetadataName, StringComparison.Ordinal);

    /// <inheritdoc cref="IsAttribute(SyntaxNode, SemanticModel, string, out AttributeSyntax, out MemberDeclarationSyntax, out ISymbol, CancellationToken)"/>
    public static bool IsAttribute(
        this SyntaxNode syntaxNode,
        SemanticModel semanticModel,
        string attributeMetadataName,
        [NotNullWhen(true)] out AttributeSyntax? attributeSyntax,
        [NotNullWhen(true)] out ISymbol? memberSymbol,
        CancellationToken cancellationToken)
        => syntaxNode.IsAttribute(semanticModel, out attributeSyntax, out _, out memberSymbol, out string? foundAttributeMetadataName, cancellationToken)
        && foundAttributeMetadataName.Equals(attributeMetadataName, StringComparison.Ordinal);
}
