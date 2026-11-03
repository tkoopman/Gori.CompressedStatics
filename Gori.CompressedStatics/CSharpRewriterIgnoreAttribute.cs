using System;
using System.Diagnostics;

namespace Gori.CompressedStatics;

/// <summary>
/// Configures a compiler diagnostic that should not block import cleanup or be reported by the rewriter.
/// Does not suppress the diagnostic during compilation.
/// </summary>
/// <param name="diagnosticID">The diagnostic ID to ignore.</param>
/// <param name="messageContains">An optional, case-sensitive substring of the invariant diagnostic message.</param>
/// <remarks>
/// The transformer loads assembly-wide rules from the host compilation.
/// Declare configuration in a separate compiled file, not in the source being rewritten.
/// </remarks>
[Conditional("Gori_CompressedStatics_EmitAttributes")]
[AttributeUsage(
    AttributeTargets.Assembly,
    AllowMultiple = true,
    Inherited = false)]
public sealed class CSharpRewriterIgnoreAttribute(string diagnosticID, string? messageContains = null) : Attribute
{
    /// <summary>
    /// Gets the diagnostic ID to ignore.
    /// </summary>
    public string DiagnosticID { get; } = diagnosticID;

    /// <summary>
    /// Gets the optional message filter. Null or whitespace matches any message with the diagnostic ID.
    /// </summary>
    public string? MessageContains { get; } = messageContains;
}
