using System.Diagnostics.CodeAnalysis;

using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics;

/// <summary>
/// Transforms input text into a string before compression.
/// </summary>
public interface IStringTransformer
{
    /// <summary>
    /// Transforms input text into the uncompressed string value.
    /// </summary>
    /// <param name="attribute">The input attribute's data.</param>
    /// <param name="compilation">The current input compilation, including project source and references, before generator outputs are added.</param>
    /// <param name="output">The transformed uncompressed value.</param>
    /// <param name="diagnostic">
    /// Outputs an optional diagnostic. Its primary reporting location is always the compression attribute.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the transformation was successful; otherwise, false.</returns>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    /// <remarks>
    /// Method must be deterministic and thread-safe. Check <paramref name="cancellationToken"/>
    /// before starting and during expensive work. Cancellation must throw
    /// <see cref="OperationCanceledException"/> rather than return false with a diagnostic.
    /// </remarks>
    bool TryTransform(
        ICompressionAttributeInfo attribute,
        Compilation compilation,
        [NotNullWhen(true)] out string? output,
        [NotNullWhen(false)] out DiagnosticInfo? diagnostic,
        CancellationToken cancellationToken);
}
