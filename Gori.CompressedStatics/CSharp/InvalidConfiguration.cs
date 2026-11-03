namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Represents invalid rewriter configuration.
/// </summary>
/// <param name="Reason">The reason the configuration is invalid.</param>
/// <param name="Value">The configuration value that caused the issue.</param>
public sealed record InvalidConfiguration(InvalidConfigurationReason Reason, string? Value) : RewriteIssue
{
    /// <summary>
    /// Gets the validation failure message, when available.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the attribute constructor parameter or named property that failed validation.
    /// </summary>
    public string? ArgumentName { get; init; }

    /// <summary>
    /// Gets the zero-based array element index, when the failure concerns an individual element.
    /// </summary>
    public int? ArgumentIndex { get; init; }

    /// <summary>
    /// Gets the failing argument or array element value, or null when unavailable or explicitly null.
    /// </summary>
    public string? ArgumentValue { get; init; }
}
