using System.Collections.Immutable;

using Gori.Common.Roslyn;

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
    ImmutableArray<CompressAttributeInfo> members) : IEquatable<TarMembers>
{
    /// <summary>
    /// Invalid null entry.
    /// </summary>
    public static readonly TarMembers Null;

    /// <summary>Gets the comparer for TAR emission inputs, excluding reporting locations.</summary>
    public static IEqualityComparer<TarMembers> EmissionComparer { get; } = new LocationIndependentComparer();

    /// <summary>
    /// Gets the tar-compressed type.
    /// </summary>
    public EquatableTypeInfo Type { get; } = type;

    /// <summary>
    /// Gets the algorithm requested on the <c>TarCompress</c> attribute.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; } = algorithm;

    /// <summary>
    /// Gets the TAR attribute's syntax location, included in default equality for diagnostic reporting.
    /// </summary>
    public LocationInfo Location { get; } = location;

    /// <summary>
    /// Gets the members to combine, in a stable order.
    /// </summary>
    public ImmutableArray<CompressAttributeInfo> Members { get; private init; } = members.IsDefault ? [] : members;

    /// <summary>Gets a value indicating whether this group may be compressed rather than emitted as empty values.</summary>
    public bool IsValid { get; private init; } = true;

    /// <summary>
    /// Gets a value indicating whether this instance is an invalid null entry.
    /// </summary>
    public bool IsNull => Type.Declarations.IsDefaultOrEmpty;

    /// <summary>Invalidates the group and releases its member payloads while retaining emission metadata.</summary>
    /// <returns>The invalid group, unchanged if already invalid.</returns>
    public TarMembers Invalidate()
        => IsValid ? this with { IsValid = false, Members = [.. Members.Select(static member => member.Invalidate())] } : this;

    /// <inheritdoc/>
    public bool Equals(TarMembers other)
        => Type.Equals(other.Type)
        && Type.Location.Equals(other.Type.Location)
        && Location.Equals(other.Location)
        && Algorithm == other.Algorithm
        && IsValid == other.IsValid
        && SequenceEquality.Equals(Members, other.Members);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TarMembers other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Type.GetHashCode();
            hashCode = (hashCode * 397) ^ (Type.Location.IsUnknown ? 0 : Type.Location.GetHashCode());
            hashCode = (hashCode * 397) ^ (Location.IsUnknown ? 0 : Location.GetHashCode());
            hashCode = (hashCode * 397) ^ (int)Algorithm;
            hashCode = (hashCode * 397) ^ IsValid.GetHashCode();
            hashCode = (hashCode * 397) ^ SequenceEquality.GetHashCode(Members);
            return hashCode;
        }
    }

    private sealed class LocationIndependentComparer : IEqualityComparer<TarMembers>
    {
        public bool Equals(TarMembers x, TarMembers y)
            => x.Type.Equals(y.Type)
            && x.Algorithm == y.Algorithm
            && x.IsValid == y.IsValid
            && x.Members.SequenceEqual(y.Members, CompressAttributeInfo.EmissionComparer);

        public int GetHashCode(TarMembers obj)
        {
            unchecked
            {
                int hashCode = (((obj.Type.GetHashCode() * 397) ^ (int)obj.Algorithm) * 397) ^ obj.IsValid.GetHashCode();
                foreach (CompressAttributeInfo member in obj.Members)
                {
                    hashCode = (hashCode * 397) ^ CompressAttributeInfo.EmissionComparer.GetHashCode(member);
                }

                return hashCode;
            }
        }
    }
}
