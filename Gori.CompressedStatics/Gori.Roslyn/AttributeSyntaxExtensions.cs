#pragma warning disable IDE0130

using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gori.Roslyn;

/// <summary>
/// Provides extension methods for working with <see cref="AttributeSyntax"/> in Roslyn.
/// </summary>
internal static class AttributeSyntaxExtensions
{
    /// <summary>
    /// Tries to retrieve a named argument from the given <see cref="AttributeSyntax"/> based on the specified argument name.
    /// </summary>
    /// <param name="attributeSyntax">The attribute syntax to search for the named argument.</param>
    /// <param name="argumentName">The name of the argument to retrieve.</param>
    /// <param name="argumentSyntax">When this method returns, contains the <see cref="AttributeArgumentSyntax"/> if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the named argument is found; otherwise, <c>false</c>.</returns>
    public static bool TryGetNamedArgument(
        this AttributeSyntax attributeSyntax,
        string argumentName,
        [NotNullWhen(true)] out AttributeArgumentSyntax? argumentSyntax)
    {
        argumentSyntax = null;
        if (attributeSyntax.ArgumentList is null)
        {
            return false;
        }

        foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList.Arguments)
        {
            if (argument.NameEquals is not null
                && argumentName.Equals(argument.NameEquals.Name.Identifier.ValueText, StringComparison.Ordinal))
            {
                argumentSyntax = argument;
                return true;
            }
        }

        return false;
    }
}
