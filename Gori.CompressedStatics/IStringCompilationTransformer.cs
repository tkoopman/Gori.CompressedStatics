using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics;

/// <summary>
/// Transforms input text into a string before compression and includes the compilation.
/// </summary>
/// <remarks>
/// This will force this transformer to re-run whenever anything in the compilation changes,
/// which includes changes to any source code or references.
/// </remarks>
public interface IStringCompilationTransformer
{
    /// <summary>
    /// Transforms input text into the uncompressed string value.
    /// </summary>
    /// <param name="attribute">The input attribute's data.</param>
    /// <param name="compilation">The current input compilation, including project source and references, before generator outputs are added.</param>
    /// <param name="output">The transformed uncompressed value.</param>
    /// <param name="diagnostics">
    /// Outputs an optional array of diagnostics. Its primary reporting location is always the compression attribute.
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
        out ImmutableArray<DiagnosticInfo> diagnostics,
        CancellationToken cancellationToken);
}
