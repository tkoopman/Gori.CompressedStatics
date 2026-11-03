using System;
using System.Diagnostics;

namespace Gori.CompressedStatics;

/// <summary>
/// Indicates that the value of the attributed property or method should be read from a file and compressed at compile time.
/// File must be a text file, and made available to the source generation. File's contents will be read as a string and compressed
/// at compile time, and the member configured to decompress and return the contents at run-time.
/// If property's type or method's return type is a byte array, the file's contents should be Base64 encoded
/// and will be decoded at compile time before compressing.
/// </summary>
/// <remarks>Adding this to a property or method that returns anything other than string or byte[] is unsupported.</remarks>
/// <param name="file">The path for the file to read the string from. File must be made available to the source generation.</param>
[Conditional("Gori_CompressedStatics_EmitAttributes")]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CompressFileAttribute(string file) : Attribute
{
    /// <summary>
    /// Gets path for file to read string from. File must be an additional file or a compiled C# source file (.cs).
    /// Additional files take precedence over compiled source files with the same path.
    /// File path is relative to the current file's directory unless RelativeToProject is true.
    /// </summary>
    public string File { get; } = file;

    /// <summary>
    /// Gets or sets a value indicating whether the file path will be relative to the project root directory.
    /// If false, the file path will be relative to the directory of the source file containing the attributed member.
    /// </summary>
    public bool RelativeToProject { get; set; }

    /// <inheritdoc cref="CompressStringAttribute.Algorithm"/>
    public CompressionAlgorithm Algorithm { get; set; }

    /// <inheritdoc cref="CompressStringAttribute.CacheMode"/>
    public CompressionCacheMode CacheMode { get; set; }

    /// <inheritdoc cref="CompressStringAttribute.Transformer"/>
    public string? Transformer { get; set; }

    /// <inheritdoc cref="CompressStringAttribute.TransformerArguments"/>
    public object[]? TransformerArguments { get; set; }
}
