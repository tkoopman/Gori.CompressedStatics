namespace Gori.CompressedStatics.Generators;

/// <summary>
/// How a member's value is physically stored in generated code.
/// </summary>
internal enum StorageKind
{
    /// <summary>
    /// Stored uncompressed (raw literal). Chosen when raw is smallest, on ties, or when empty.
    /// </summary>
    Raw,

    /// <summary>
    /// Compressed with Brotli.
    /// </summary>
    Brotli,

    /// <summary>
    /// Compressed with GZip.
    /// </summary>
    GZip,

    /// <summary>
    /// Compressed with Deflate.
    /// </summary>
    Deflate,
}
