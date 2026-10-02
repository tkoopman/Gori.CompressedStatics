using System.IO.Compression;
using System.Reflection;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Contains the source code templates used by the compression generators.
/// </summary>
internal class SourceTemplates
{
    /// <summary>
    /// Contains the source code for the attribute markers used by the compression generators.
    /// </summary>
    public static readonly string AttributeMarkers;

    /// <summary>
    /// Contains the source code for the DecompressStatics class used for decompressing static data.
    /// </summary>
    public static readonly string DecompressStatics;

    /// <summary>
    /// Initializes static members of the <see cref="SourceTemplates"/> class.
    /// </summary>
    static SourceTemplates()
    {
        Assembly assembly = typeof(SourceTemplates).Assembly;
        string resourceName = "Gori.CompressedStatics.Source.Templates.zip";

        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
        ZipArchiveEntry attributeMarkersEntry = zipArchive.GetEntry("AttributeMarkers.cs");
        using (Stream entryStream = attributeMarkersEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            AttributeMarkers = reader.ReadToEnd();
        }

        ZipArchiveEntry decompressStaticsEntry = zipArchive.GetEntry("DecompressStatics.cs");
        using (Stream entryStream = decompressStaticsEntry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            DecompressStatics = reader.ReadToEnd();
        }
    }
}
