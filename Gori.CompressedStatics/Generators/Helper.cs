using System.Diagnostics;
using System.Globalization;
using System.Text;

using Gori.Roslyn;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Path helpers shared by the V2 generator pipeline.
/// </summary>
internal static class Helper
{
    /// <summary>
    /// Appends the opening namespace and type declarations to the given <see cref="CodeStringBuilder.CodeStringBuilder"/>.
    /// </summary>
    /// <param name="builder">The code string builder to append to.</param>
    /// <param name="type">The type information to use for generating the declarations.</param>
    public static void AppendOpen(CodeStringBuilder.CodeStringBuilder builder, EquatableTypeInfo type)
    {
        if (!string.IsNullOrEmpty(type.Namespace))
        {
            _ = builder.WriteLine($"namespace {type.Namespace}")
                       .WriteLine("{");
        }

        foreach (TypeDeclaration declaration in type.Declarations)
        {
            _ = builder.WriteLine($"partial {declaration.Keyword} {declaration.Name}")
                       .WriteLine("{");
        }
    }

    /// <summary>
    /// Appends the closing type and namespace declarations to the given <see cref="CodeStringBuilder.CodeStringBuilder"/>.
    /// </summary>
    /// <param name="builder">The code string builder to append to.</param>
    /// <param name="type">The type information to use for generating the declarations.</param>
    public static void AppendClose(CodeStringBuilder.CodeStringBuilder builder, EquatableTypeInfo type)
    {
        for (int i = 0; i < type.Declarations.Length; i++)
        {
            _ = builder.WriteLine("}");
        }

        if (!string.IsNullOrEmpty(type.Namespace))
        {
            _ = builder.WriteLine("}");
        }
    }

    /// <summary>
    /// Returns the C# keyword for the given return type.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <returns>The C# keyword for the return type.</returns>
    public static string AsKeyword(this ReturnType returnType)
        => returnType is ReturnType.String ? "string" : "byte[]";

    /// <summary>
    /// Builds a C# literal representation of a byte array.
    /// </summary>
    /// <param name="data">The byte array to build a literal for.</param>
    /// <returns>The C# literal representation of the byte array.</returns>
    public static string BuildByteArrayLiteral(this byte[] data)
        => data.Length == 0 ? Const.EmptyByteArray : "[" + BuildByteArrayInner(data) + "]";

    /// <summary>
    /// Builds a C# call to the decompression method for the given return type, storage kind, and compressed field name.
    /// </summary>
    /// <param name="returnType">The return type.</param>
    /// <param name="storage">The storage kind.</param>
    /// <param name="compressedFieldName">The name of the compressed field.</param>
    /// <returns>The C# call to the decompression method.</returns>
    public static string BuildDecompressCall(ReturnType returnType, StorageKind storage, string compressedFieldName)
    {
        Debug.Assert(storage is not StorageKind.Raw, "StorageKind.Raw is not supported for decompression.");
        string method = returnType is ReturnType.String ? "AsString" : "AsBytes";
        return $"{Const.DecompressStaticsClassFullName}.{method}({compressedFieldName}, {Const.AlgorithmEnumFullName}.{storage})";
    }

    /// <summary>
    /// Sanitizes a string by replacing all non-alphanumeric characters with underscores.
    /// </summary>
    /// <param name="raw">The string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    public static string Sanitize(string raw)
    {
        var builder = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            _ = builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Converts a <see cref="CompressionAlgorithm"/> to a <see cref="StorageKind"/>.
    /// </summary>
    /// <param name="algorithm">The compression algorithm.</param>
    /// <returns>The corresponding storage kind.</returns>
    public static StorageKind ToStorageKind(this CompressionAlgorithm algorithm)
        => algorithm switch
        {
            CompressionAlgorithm.Brotli => StorageKind.Brotli,
            CompressionAlgorithm.GZip => StorageKind.GZip,
            CompressionAlgorithm.Deflate => StorageKind.Deflate,
            _ => StorageKind.Raw,
        };

    /// <summary>
    /// Attempts to decode a base64 string. Whitespace is trimmed first. Returns <see langword="false"/>
    /// (with an empty array) when the value is not valid base64.
    /// </summary>
    /// <param name="text">The base64 string to decode.</param>
    /// <param name="bytes">The decoded byte array.</param>
    /// <returns><see langword="true"/> if the string was successfully decoded; otherwise, <see langword="false"/>.</returns>
    public static bool TryConvertFromBase64(string text, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(text.Trim());
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static string BuildByteArrayInner(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0)
            {
                _ = builder.Append(", ");
            }

            _ = builder.Append(data[i].ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
