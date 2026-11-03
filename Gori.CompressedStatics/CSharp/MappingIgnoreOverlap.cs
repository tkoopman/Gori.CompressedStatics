namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Warns that an explicit mapping rewrote a reference whose diagnostic also matched an ignore.
/// The mapping still applies; this warning does not block import cleanup.
/// </summary>
/// <param name="Reference">The original reference that was rewritten.</param>
/// <param name="Destination">The mapped destination.</param>
/// <param name="DiagnosticID">The original compiler diagnostic ID matched by the ignore.</param>
/// <param name="MessageContains">The matched ignore's message filter, or null for an ID-only ignore.</param>
public sealed record MappingIgnoreOverlap(string Reference, string Destination, string DiagnosticID, string? MessageContains) : RewriteIssue;
