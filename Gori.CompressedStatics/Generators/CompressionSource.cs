namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Identifies which attribute produced a member input.
/// </summary>
internal enum CompressionSource
{
    /// <summary>
    /// Value came from a <c>CompressString</c> attribute.
    /// </summary>
    String,

    /// <summary>
    /// Value came from a <c>CompressFile</c> attribute (an additional text file).
    /// </summary>
    File,
}
