namespace Gori.CompressedStatics;

/// <summary>
/// Identifies the type of compress attribute being analyzed.
/// </summary>
internal enum CompressAttributeType
{
    /// <summary>
    /// The attribute is not recognized as a compress attribute.
    /// </summary>
    Unknown,

    /// <summary>
    /// The attribute is a <c>CompressString</c> attribute.
    /// </summary>
    CompressString,

    /// <summary>
    /// The attribute is a <c>CompressFile</c> attribute.
    /// </summary>
    CompressFile,

    /// <summary>
    /// The attribute is a <c>TarCompress</c> attribute.
    /// </summary>
    TarCompress,
}
