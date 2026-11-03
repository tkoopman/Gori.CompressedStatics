namespace Gori.CompressedStatics;

/// <summary>
/// Controls how source-generated property values are cached.
/// </summary>
public enum CompressionCacheMode
{
    /// <summary>
    /// Decompress once during type initialization and keep the value.
    /// </summary>
    OnInit,

    /// <summary>
    /// Lazily initialize on first property access without synchronization.
    /// </summary>
    Lazy,

    /// <summary>
    /// Lazily initialize on first access using <see cref="System.Lazy{T}"/> for thread safety.
    /// </summary>
    LazyThreadSafe,
}
