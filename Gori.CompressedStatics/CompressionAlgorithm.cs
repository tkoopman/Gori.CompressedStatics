namespace Gori.CompressedStatics;

/// <summary>
/// Specifies the compression algorithm to use.
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// Auto select best available algorithm, including leaving uncompressed if that is smallest.
    /// </summary>
    Auto,

    /// <summary>
    /// Auto select best available algorithm, but do not leave uncompressed even if that is smallest.
    /// </summary>
    AutoForced,

    /// <summary>
    /// Brotli compression algorithm.
    /// </summary>
    Brotli,

    /// <summary>
    /// GZip compression algorithm.
    /// </summary>
    GZip,

    /// <summary>
    /// Deflate compression algorithm.
    /// </summary>
    Deflate,
}
