namespace Gori.CompressedStatics;

#pragma warning disable SA1600 // Elements should be documented
internal static class Const
{
    /*
     * General Constants
     */

    public const string Namespace = "Gori.CompressedStatics";
    public const string HintSuffix = "CompressedStatics";

    /*
     * Attribute names
     */

    public const string CompressFileAttributeShortName = "CompressFile";
    public const string CompressFileAttributeName = nameof(CompressFileAttribute);
    public const string CompressFileAttributeFullShortName = $"{Namespace}.{CompressFileAttributeShortName}";
    public const string CompressFileAttributeFullName = $"{Namespace}.{CompressFileAttributeName}";
    public const string CompressStringAttributeShortName = "CompressString";
    public const string CompressStringAttributeName = nameof(CompressStringAttribute);
    public const string CompressStringAttributeFullShortName = $"{Namespace}.{CompressStringAttributeShortName}";
    public const string CompressStringAttributeFullName = $"{Namespace}.{CompressStringAttributeName}";
    public const string TarCompressAttributeShortName = "TarCompress";
    public const string TarCompressAttributeName = nameof(TarCompressAttribute);
    public const string TarCompressAttributeFullShortName = $"{Namespace}.{TarCompressAttributeShortName}";
    public const string TarCompressAttributeFullName = $"{Namespace}.{TarCompressAttributeName}";

    /*
     * Analyzer Diagnostic IDs
     */

    public const string DiagnosticDescriptorCategory = Namespace;
    public const string DiagnosticDescriptorIDPrefix = "GCS";

    /*
     * Stream Types
     */

    public const string BrotliStreamPrefix = "new global::";
    public const string BrotliStreamName20 = "Brotli.BrotliStream";
    public const string BrotliStreamName21 = "System.IO.Compression.BrotliStream";
    public const string BrotliStreamSuffix = "(memoryStream, global::System.IO.Compression.CompressionMode.Decompress)";
    public const string BrotliStreamNotSupported = """throw new global::System.InvalidOperationException("Unsupported compression algorithm: Brotli")""";

    /*
     * Code Generation Gori.DecompressStatics
     */

    public const string DecompressStaticsNamespace = "Gori.DecompressStatics";
    public const string DecompressStaticsHintName = $"{DecompressStaticsNamespace}.g.cs";
    public const string DecompressStaticsClassName = "DecompressStatic";
    public const string DecompressStaticsClassFullName = $"{Global}{DecompressStaticsNamespace}.{DecompressStaticsClassName}";
    public const string AlgorithmEnumName = "Algorithm";
    public const string AlgorithmEnumFullName = $"{Global}{DecompressStaticsNamespace}.{AlgorithmEnumName}";
    public const string DecompressStaticsBrotliVariableName = "BrotliStream";

    /*
     * Code Generation Member Names
     */

    public const string CacheFieldNamePrefix = "__uncompressed_";
    public const string CompressedFieldNamePrefix = "__compressed_";

    public const string DecompressedTarFieldName = "tarUncompressed";
    public const string TarCompressedFieldName = "__tarCompressed";

    // TAR buffer, use count and method name are used to manage
    // the lifetime of the decompressed tar data when user defined
    // static constructor exists.
    public const string TarBufferFieldName = $"__{DecompressedTarFieldName}";
    public const string TarBufferUseCountFieldName = $"__{DecompressedTarFieldName}Count";
    public const string TarBufferMethodName = "__GetDecompressedTar";

    /*
     * Code Generation Parts
     */
    public const string EmptyByteArray = "[]";
    public const string Global = "global::";
}
