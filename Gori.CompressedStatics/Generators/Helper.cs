using System.Diagnostics;
using System.Globalization;
using System.Text;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis.CSharp;

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
            _ = builder.WriteLine($"partial {declaration.Keyword} {EscapeTypeDeclarationName(declaration.Name)}")
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
    /// Escapes keyword identifiers for use in generated C# source.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <returns>The identifier with a verbatim prefix when needed.</returns>
    public static string EscapeIdentifier(string name)
        => SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
            ? "@" + name
            : name;

    /// <summary>
    /// Encodes names without collapsing distinct characters to the same output.
    /// </summary>
    /// <param name="raw">The name to encode.</param>
    /// <param name="preserveSeparators">Whether to retain dots and hyphens in file hint names.</param>
    /// <returns>The encoded name.</returns>
    public static string Sanitize(string raw, bool preserveSeparators = false)
    {
        var builder = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            _ = c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9')
                || (preserveSeparators && c is '.' or '-')
                ? builder.Append(c)
                : builder.Append("_u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
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

    private static string EscapeTypeDeclarationName(string name)
    {
        int parameterStart = name.IndexOf('<');
        return parameterStart < 0
            ? EscapeIdentifier(name)
            : EscapeIdentifier(name[..parameterStart]) + "<"
                + string.Join(", ", name[(parameterStart + 1)..^1].Split(',').Select(static parameter => EscapeIdentifier(parameter.Trim()))) + ">";
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
