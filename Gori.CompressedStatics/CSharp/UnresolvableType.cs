namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Indicates what was unresolvable.
/// </summary>
public enum UnresolvableType
{
    /// <summary>
    /// Type that was not able to be resolved by provided compilation.
    /// </summary>
    UnresolvableType,

    /// <summary>
    /// Extension that was not able to be resolved by provided compilation.
    /// </summary>
    UnresolvableExtension,

    /// <summary>
    /// Multiple matching types found in provided compilation.
    /// </summary>
    AmbiguousType,
}
