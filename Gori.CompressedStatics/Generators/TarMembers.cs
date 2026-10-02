using System.Collections.Immutable;

using Gori.Roslyn;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// A tar type together with the members that will be combined into its archive. Produced after
/// members have been grouped by their containing type. Carries only the raw member inputs; the
/// archive is merged and compressed at emit time so the compressed bytes are never cached.
/// </summary>
internal readonly struct TarMembers(
    EquatableTypeInfo type,
    CompressionAlgorithm algorithm,
    LocationInfo location,
    ImmutableArray<CompressionMemberInput> members) : IEquatable<TarMembers>
{
    /// <summary>
    /// Gets the tar-compressed type.
    /// </summary>
    public EquatableTypeInfo Type { get; } = type;

    /// <summary>
    /// Gets the algorithm requested on the <c>TarCompress</c> attribute.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; } = algorithm;

    /// <summary>
    /// Gets the member's source location, used when reporting diagnostics. Deliberately
    /// excluded from equality so line/column shifts from unrelated edits do not bust the incremental
    /// cache and force re-compression of unchanged payloads.
    /// </summary>
    public LocationInfo Location { get; } = location;

    /// <summary>
    /// Gets the members to combine, in a stable order.
    /// </summary>
    public ImmutableArray<CompressionMemberInput> Members { get; } = members.IsDefault ? [] : members;

    /// <inheritdoc/>
    public bool Equals(TarMembers other)
        => Type.Equals(other.Type)
        && Algorithm == other.Algorithm
        && SequenceEquality.Equals(Members, other.Members);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TarMembers other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Type.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Algorithm;
            hashCode = (hashCode * 397) ^ SequenceEquality.GetHashCode(Members);
            return hashCode;
        }
    }
}
