namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Indicates the return type of a method or property with a compress attribute.
/// </summary>
internal enum ReturnType
{
    /// <summary>
    /// The return type is not supported for compression. This may be due to the type being unsupported or the method/property not being a valid candidate for compression.
    /// </summary>
    Unsupported,

    /// <summary>
    /// The return type is a string.
    /// </summary>
    String,

    /// <summary>
    /// The return type is a byte array.
    /// </summary>
    ByteArray,
}
