namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Type of transformer compress attribute linked to.
/// </summary>
[Flags]
internal enum TransformerType
{
    /// <summary>
    /// No transformer configured.
    /// </summary>
    None,

    /// <summary>
    /// Basic string transformer.
    /// </summary>
    String = 1 << 0,

    /// <summary>
    /// Basic binary transformer.
    /// </summary>
    Binary = 1 << 1,

    /// <summary>
    /// INVALID by self. Flag part of all compilation type transformers.
    /// </summary>
    Compilation = 1 << 2,

    /// <summary>
    /// Compilation string transformer.
    /// </summary>
    StringCompilation = String | Compilation,

    /// <summary>
    /// Compilation binary transformer.
    /// </summary>
    BinaryCompilation = Binary | Compilation,

    /// <summary>
    /// Transformer specified could not be found.
    /// Either type not found or doesn't implement correct transformer interface.
    /// </summary>
    NotFound = int.MaxValue,
}
