namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Indicates the level of Brotli support available in the current environment.
/// </summary>
internal enum BrotliSupport
{
    /// <summary>
    /// No Brotli support is available; Brotli compression will not be performed.
    /// </summary>
    None,

    /// <summary>
    /// Legacy Brotli support is available via the Brotli NuGet package (BrotliSharpLib).
    /// This is used for .NET Standard 2.0 and earlier.
    /// </summary>
    Legacy,

    /// <summary>
    /// Modern Brotli support is available. This is used for .NET Core 3.0 and later.
    /// </summary>
    Modern,
}
