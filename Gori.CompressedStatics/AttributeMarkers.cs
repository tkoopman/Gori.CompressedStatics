#pragma warning disable IDE0001, IDE0002, SA1402, SA1649

namespace Gori.CompressedStatics;

/// <summary>
/// Specifies the compression algorithm to use.
/// </summary>
#if !Gori_CompressedStatics_EmitAttributes
[global::Microsoft.CodeAnalysis.Embedded]
#endif
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

/// <summary>
/// Controls how source-generated property values are cached.
/// </summary>
#if !Gori_CompressedStatics_EmitAttributes
[global::Microsoft.CodeAnalysis.Embedded]
#endif
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
    /// Lazily initialize on first access using <see cref="global::System.Lazy{T}"/> for thread safety.
    /// </summary>
    LazyThreadSafe,
}

/// <summary>
/// Indicates that the string value of the property or method should be compressed at compile time using the specified compression algorithm and cache mode.
/// </summary>
/// <param name="value">The string value to be compressed.</param>
#if !Gori_CompressedStatics_EmitAttributes
[global::Microsoft.CodeAnalysis.Embedded]
#endif
[global::System.Diagnostics.Conditional("Gori_CompressedStatics_EmitAttributes")]
[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CompressStringAttribute(string value) : global::System.Attribute
{
    /// <summary>
    /// Gets the string value to be compressed at compile time. If the
    /// property's type or method's return type is a byte array, the string
    /// value should be Base64 encoded and will be decoded at compile time
    /// before compressing.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Gets or sets the compression algorithm to use for compressing the string.
    /// Ignored if attribute is applied to a property that is covered by a <see cref="TarCompressAttribute"/>
    /// on the containing class.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the cache mode to use for compressing the string.
    /// Setting this to non-default values will exclude this member from any <see cref="TarCompressAttribute"/>.
    /// </summary>
    public CompressionCacheMode CacheMode { get; set; }
}

/// <summary>
/// Indicates that the value of the attributed property or method should be read from a file and compressed at compile time.
/// File must be a text file, and made available to the source generation. File's contents will be read as a string and compressed
/// at compile time, and the member configured to decompress and return the contents at run-time.
/// If property's type or method's return type is a byte array, the file's contents should be Base64 encoded
/// and will be decoded at compile time before compressing.
/// </summary>
/// <remarks>Adding this to a property or method that returns anything other than string or byte[] is unsupported.</remarks>
/// <param name="file">The path for the file to read the string from. File must be made available to the source generation.</param>
#if !Gori_CompressedStatics_EmitAttributes
[global::Microsoft.CodeAnalysis.Embedded]
#endif
[global::System.Diagnostics.Conditional("Gori_CompressedStatics_EmitAttributes")]
[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CompressFileAttribute(string file) : global::System.Attribute
{
    /// <summary>
    /// Gets path for file to read string from. File must be set as an additional file in the project.
    /// File path is relative to the current file's directory.
    /// </summary>
    public string File { get; } = file;

    /// <summary>
    /// Gets or sets a value indicating whether the file path will be relative to the project root directory.
    /// If false, the file path will be relative to the directory of the source file containing the attributed member.
    /// </summary>
    public bool RelativeToProject { get; set; }

    /// <summary>
    /// Gets or sets the compression algorithm to use for compressing the string.
    /// Leave on auto and during compile time the best available algorithm will be chosen,
    /// including leaving uncompressed if that is smallest.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the cache mode used by source-generated properties.
    /// Methods are always generated to re-decompress on each call.
    /// </summary>
    public CompressionCacheMode CacheMode { get; set; }
}

/// <summary>
/// For all compress attributes assigned to properties in a class or struct,
/// all uncompressed data will be tarred into a single stream first then compressed.
/// This may provide better overall compression but means all need to be decompressed at once,
/// and this happens at class initialization. So great if all or most data is going to be read.
///
/// Notes:
///     When assigned to a class or struct, all compressed properties are tarred together and compressed
///     unless excluded by name.
///
///     Attributes assigned to methods are not affected by this attribute, and will still
///     be compressed individually and decompressed on each call.
///
///     Algorithm on individual attributes will be ignored and value assigned here used.
///
///     When the class does not already declare a static constructor, tarred members may be read-only
///     auto properties or partial; the generator assigns them in a generated static constructor.
///
///     When the class already declares a static constructor, every tarred member must be declared
///     partial; the generator loads their values through generated backing-field initializers, so no
///     call into the generated code is required from the user's static constructor.
///
///     Does not tar properties across different classes or structs.
/// </summary>
#if !Gori_CompressedStatics_EmitAttributes
[global::Microsoft.CodeAnalysis.Embedded]
#endif
[global::System.Diagnostics.Conditional("Gori_CompressedStatics_EmitAttributes")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class TarCompressAttribute : global::System.Attribute
{
    /// <summary>
    /// Gets or sets the compression algorithm to use for compressing the tarred properties.
    /// Leave on auto and during compile time the best available algorithm will be chosen,
    /// including leaving uncompressed if that is smallest.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; set; }

    /// <summary>
    /// Gets or sets compressed property names that should be excluded from tar compression
    /// and handled with normal per-member compression behavior.
    /// Properties can also be excluded if assigned a CacheMode other than <see cref="CompressionCacheMode.OnInit"/>.
    /// </summary>
    public string[] Exclude { get; set; } = [];
}
