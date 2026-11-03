using System;
using System.Diagnostics;

namespace Gori.CompressedStatics;

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
[Conditional("Gori_CompressedStatics_EmitAttributes")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class TarCompressAttribute : Attribute
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
