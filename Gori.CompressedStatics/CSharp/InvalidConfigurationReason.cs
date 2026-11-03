namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Indicates why rewriter configuration is invalid.
/// </summary>
public enum InvalidConfigurationReason
{
    /// <summary>
    /// A configuration value is invalid.
    /// </summary>
    InvalidValue,

    /// <summary>
    /// Configuration rules in the same scope conflict.
    /// </summary>
    ConflictingRules,

    /// <summary>
    /// A mapping cannot preserve generic arguments on containing types within its matched source prefix.
    /// Map the outer type instead to preserve its generic arguments and nested types.
    /// </summary>
    UnsupportedContainingTypeArguments,
}
