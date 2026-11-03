using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Gori.CompressedStatics.Generators;

namespace Gori.CompressedStatics;

/// <summary>
/// Transforms input text into bytes before compression.
/// </summary>
public interface IBinaryTransformer
{
    /// <inheritdoc cref="IBinaryCompilationTransformer.TryTransform"/>
    bool TryTransform(
        ICompressionAttributeInfo attribute,
        [NotNullWhen(true)] out byte[]? output,
        out ImmutableArray<DiagnosticInfo> diagnostics,
        CancellationToken cancellationToken);
}
