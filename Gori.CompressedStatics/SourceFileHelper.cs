using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics;

/// <summary>
/// Finds compiled C# source files for CompressFile resolution and validation.
/// </summary>
internal static class SourceFileHelper
{
    /// <summary>
    /// Finds the syntax tree matching an absolute C# file path.
    /// </summary>
    /// <param name="compilation">The compilation to search.</param>
    /// <param name="absoluteFilePath">The normalized absolute path to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching tree, or null if the file is not a compiled C# source file.</returns>
    public static SyntaxTree? FindSyntaxTree(Compilation compilation, string absoluteFilePath, CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(absoluteFilePath), ".cs", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(tree.FilePath)
                && StringComparer.Ordinal.Equals(Path.GetFullPath(tree.FilePath), absoluteFilePath))
            {
                return tree;
            }
        }

        return null;
    }
}
