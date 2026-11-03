using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Gori.CompressedStatics.Generators;

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
        [NotNullWhen(true)] out byte[]? output,
        out ImmutableArray<DiagnosticInfo> diagnostics,
        CancellationToken cancellationToken)
    {
        if (attribute is null || attribute.SourceValue is null || attribute.SourceValue.IsWhiteSpace())
        {
            output = [];
            diagnostics = default;
            return true;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            output = Convert.FromBase64String(attribute.SourceValue);
            diagnostics = default;
            return true;
        }
        catch (FormatException)
        {
            output = null;
            diagnostics = [
                new DiagnosticInfo(
                    DiagnosticDescriptors.InvalidBase64String,
                    attribute.MemberName)];

            return false;
        }
    }
}
