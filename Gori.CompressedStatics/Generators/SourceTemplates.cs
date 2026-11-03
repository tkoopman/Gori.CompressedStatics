using System.IO.Compression;
using System.Reflection;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Contains the source code templates used by the compression generators.
/// </summary>
internal class SourceTemplates
{
    /// <summary>
    /// Contains the source code for the compression algorithm enum used by compression attributes.
    /// </summary>
    public static readonly string CompressionAlgorithm;

    /// <summary>
    /// Contains the source code for the compression cache mode enum used by compression attributes.
    /// </summary>
    public static readonly string CompressionCacheMode;

    /// <summary>
    /// Contains the source code for the string compression attribute.
    /// </summary>
    public static readonly string CompressStringAttribute;

    /// <summary>
    /// Contains the source code for the file compression attribute.
    /// </summary>
    public static readonly string CompressFileAttribute;

    /// <summary>
    /// Contains the source code for the tar compression attribute.
    /// </summary>
    public static readonly string TarCompressAttribute;

    /// <summary>
    /// Contains the source code for the DecompressStatics class used for decompressing static data.
    /// </summary>
    public static readonly string DecompressStatics;

    /// <summary>
    /// Contains the source code for the ignore attribute used by the compression generators.
    /// </summary>
    public static readonly string CSharpRewriterIgnoreAttribute;

    /// <summary>
    /// Contains the source code for the options used by the compression generators.
    /// </summary>
    public static readonly string CSharpRewriterOptions;

    /// <summary>
    /// Contains the source code for the type mapping attribute used by the compression generators.
    /// </summary>
    public static readonly string CSharpRewriterTypeMappingAttribute;

    /// <summary>
    /// Initializes static members of the <see cref="SourceTemplates"/> class.
    /// </summary>
    static SourceTemplates()
    {
        Assembly assembly = typeof(SourceTemplates).Assembly;
        string resourceName = "Gori.CompressedStatics.Source.Templates.zip";

        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
        ZipArchiveEntry compressionAlgorithmEntry = zipArchive.GetEntry("CompressionAlgorithm.cs");
        using (Stream entryStream = compressionAlgorithmEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            CompressionAlgorithm = reader.ReadToEnd();
        }

        ZipArchiveEntry compressionCacheModeEntry = zipArchive.GetEntry("CompressionCacheMode.cs");
        using (Stream entryStream = compressionCacheModeEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            CompressionCacheMode = reader.ReadToEnd();
        }

        ZipArchiveEntry compressStringAttributeEntry = zipArchive.GetEntry("CompressStringAttribute.cs");
        using (Stream entryStream = compressStringAttributeEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            CompressStringAttribute = reader.ReadToEnd();
        }

        ZipArchiveEntry compressFileAttributeEntry = zipArchive.GetEntry("CompressFileAttribute.cs");
        using (Stream entryStream = compressFileAttributeEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            CompressFileAttribute = reader.ReadToEnd();
        }

        ZipArchiveEntry tarCompressAttributeEntry = zipArchive.GetEntry("TarCompressAttribute.cs");
        using (Stream entryStream = tarCompressAttributeEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            TarCompressAttribute = reader.ReadToEnd();
        }

        ZipArchiveEntry decompressStaticsEntry = zipArchive.GetEntry("DecompressStatics.cs");
        using (Stream entryStream = decompressStaticsEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            DecompressStatics = reader.ReadToEnd();
        }

        ZipArchiveEntry ignoreAttributeEntry = zipArchive.GetEntry("CSharpRewriterIgnoreAttribute.cs");
        using (Stream entryStream = ignoreAttributeEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            CSharpRewriterIgnoreAttribute = reader.ReadToEnd();
        }

        ZipArchiveEntry optionsEntry = zipArchive.GetEntry("CSharpRewriterOptions.cs");
        using (Stream entryStream = optionsEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            CSharpRewriterOptions = reader.ReadToEnd();
        }

        ZipArchiveEntry typeMappingAttributeEntry = zipArchive.GetEntry("CSharpRewriterTypeMappingAttribute.cs");
        using (Stream entryStream = typeMappingAttributeEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            CSharpRewriterTypeMappingAttribute = reader.ReadToEnd();
        }
    }
}
