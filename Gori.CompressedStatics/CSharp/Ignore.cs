using System.Collections.Immutable;
using System.Diagnostics;

namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Diagnostic errors that should not prevent the rewriter from cleaning up import statements, and should not be reported to the user.
/// </summary>
/// <param name="diagnosticID">The ID of the diagnostic to ignore.</param>
/// <param name="messageContains">A substring that should be present in the diagnostic message for it to be ignored.</param>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class Ignore(string diagnosticID, string? messageContains = null)
{
    private static readonly Lazy<ImmutableArray<Ignore>> PolyfillIgnoresLazy = new Lazy<ImmutableArray<Ignore>>(GetPolyfillIgnores);

    /// <summary>
    /// Gets ignores required when using libraries like PolySharp.
    /// </summary>
    public static ImmutableArray<Ignore> PolyfillIgnores => PolyfillIgnoresLazy.Value;

    /// <summary>
    /// Gets the diagnostic ID to ignore.
    /// </summary>
    public string DiagnosticID { get; } = diagnosticID ?? throw new ArgumentNullException(nameof(diagnosticID));

    /// <summary>
    /// Gets the substring that must be found in the diagnostic message to be ignored. If null only needs to match the ID.
    /// </summary>
    public string? MessageContains { get; } = messageContains is null || messageContains.IsWhiteSpace() ? null : messageContains;

    /// <summary>
    /// Checks equality to another Ignore.
    /// </summary>
    /// <param name="other">The other Ignore to compare to.</param>
    /// <returns>True if both are equal.</returns>
    public bool Equals(Ignore? other)
        => other is not null
        && StringComparer.Ordinal.Equals(DiagnosticID, other.DiagnosticID)
        && StringComparer.Ordinal.Equals(MessageContains, other.MessageContains);

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as Ignore);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + StringComparer.Ordinal.GetHashCode(DiagnosticID);
            hash = (hash * 23) + (MessageContains is null ? 0 : StringComparer.Ordinal.GetHashCode(MessageContains));
            return hash;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => DiagnosticID + (MessageContains is null ? string.Empty : $" containing '{MessageContains}'");

    private static ImmutableArray<Ignore> GetPolyfillIgnores()
        => [
            new("CS0656", "System.Runtime.CompilerServices.RequiredMemberAttribute"),
            new("CS0656", "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute"),
            new("CS0656", "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute"),
            new("CS0656", "System.Runtime.CompilerServices.IsExternalInit"),
            new("CS0518", "System.Runtime.CompilerServices.RequiredMemberAttribute"),
            new("CS0518", "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute"),
            new("CS0518", "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute"),
            new("CS0518", "System.Runtime.CompilerServices.IsExternalInit"),
        ];

    private string GetDebuggerDisplay() => ToString();
}
