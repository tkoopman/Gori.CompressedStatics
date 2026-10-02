#pragma warning disable IDE0130

namespace Gori.Roslyn;

/// <summary>
/// Provides helper methods for working with Roslyn symbols and syntax nodes.
/// </summary>
internal static class RoslynHelper
{
    /// <summary>
    /// Extracts the type name from a fully qualified type name.
    /// </summary>
    /// <param name="fullName">The fully qualified type name.</param>
    /// <returns>The type name without the namespace.</returns>
    public static string GetNameFromQualifiedName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            return fullName;
        }

        int lastDotIndex = fullName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullName[(lastDotIndex + 1)..] : fullName;
    }
}
