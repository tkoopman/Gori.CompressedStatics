using System.Diagnostics.CodeAnalysis;

using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics;

/// <summary>
/// Converts from a Base64 input string to byte[].
/// </summary>
public class Base64Transformer : IBinaryTransformer
{
    /// <summary>
    /// Instance of Base64 input transformer.
    /// </summary>
    public static readonly Base64Transformer Instance = new();

    /// <inheritdoc/>
    public bool TryTransform(
        ICompressionAttributeInfo? attribute,
        Compilation compilation,
        [NotNullWhen(true)] out byte[]? output,
        [NotNullWhen(false)] out DiagnosticInfo? diagnostic,
        CancellationToken cancellationToken)
    {
        if (attribute is null || string.IsNullOrWhiteSpace(attribute.SourceValue))
        {
            output = [];
            diagnostic = null;
            return true;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            output = Convert.FromBase64String(attribute.SourceValue);
            diagnostic = null;
            return true;
        }
        catch (FormatException)
        {
            output = null;
            diagnostic = new DiagnosticInfo(
                DiagnosticDescriptors.InvalidBase64String,
                attribute.MemberName);

            return false;
        }
    }
}
