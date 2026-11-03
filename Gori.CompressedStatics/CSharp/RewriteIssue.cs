using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Represents an issue encountered while configuring or performing a rewrite.
/// </summary>
public abstract record RewriteIssue
{
    /// <summary>
    /// Gets the source location, or null when the issue has no source location.
    /// </summary>
    public Location? Location { get; init; }
}
