namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Represents an unhandled compiler error in the source being rewritten.
/// </summary>
/// <param name="DiagnosticID">The compiler diagnostic ID.</param>
/// <param name="Message">The invariant compiler diagnostic message.</param>
public sealed record BindingIssue(string DiagnosticID, string Message) : RewriteIssue;
