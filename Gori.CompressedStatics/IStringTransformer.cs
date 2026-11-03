using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Gori.CompressedStatics.Generators;

namespace Gori.CompressedStatics;

/// <summary>
/// Transforms input text into a string before compression.
/// </summary>
public interface IStringTransformer
{
    /// <inheritdoc cref="IStringCompilationTransformer.TryTransform"/>
    bool TryTransform(
        ICompressionAttributeInfo attribute,
        [NotNullWhen(true)] out string? output,
        out ImmutableArray<DiagnosticInfo> diagnostics,
        CancellationToken cancellationToken);
}
