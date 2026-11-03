using System.Collections.Immutable;
using System.Text;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// A symbol-free description of a single member to compress, for Roslyn value caching.
///
/// Stores either source text or processed binary data, never both. Binary payloads are compared by
/// content so newly allocated but identical transformer results preserve incremental caching.
///
/// Default equality includes locations. Pipeline-specific comparers exclude source positions during
/// file resolution or all locations during emission, without caching stale diagnostic locations.
/// </summary>
internal readonly struct CompressAttributeInfo : IEquatable<CompressAttributeInfo>, ICompressionAttributeInfo
{
    /// <summary>
    /// Null instance.
    /// </summary>
    public static readonly CompressAttributeInfo Null;

    /// <summary>Initializes a new instance of the <see cref="CompressAttributeInfo"/> struct with valid processing state.</summary>
    public CompressAttributeInfo()
    {
    }

    /// <summary>Gets the comparer for file-resolution inputs, excluding source positions.</summary>
    public static IEqualityComparer<CompressAttributeInfo> FilePathComparer { get; } = new LocationComparer(true);

    /// <summary>Gets the comparer for emission inputs, excluding reporting locations.</summary>
    public static IEqualityComparer<CompressAttributeInfo> EmissionComparer { get; } = new LocationComparer(false);

    /// <summary>
    /// Gets the symbol-free description of the type declaring this member.
    /// </summary>
    public required EquatableTypeInfo ContainingType { get; init; }

    /// <summary>
    /// Gets the member name.
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// Gets whether the member is a property or a method.
    /// </summary>
    public required MemberKind MemberKind { get; init; }

    /// <summary>
    /// Gets the member accessibility keyword, for example <c>public</c>.
    /// </summary>
    public required string Accessibility { get; init; }

    /// <summary>
    /// Gets a value indicating whether the member is declared <c>partial</c> (drives body vs field emission).
    /// </summary>
    public required bool IsPartial { get; init; }

    /// <summary>
    /// Gets the member return type (string or byte[]).
    /// </summary>
    public required ReturnType ReturnType { get; init; }

    /// <summary>
    /// Gets which attribute produced this member.
    /// </summary>
    public required CompressAttributeType AttributeType { get; init; }

    /// <summary>
    /// Gets the algorithm requested via the attribute (may be <c>Auto</c>).
    /// </summary>
    public CompressionAlgorithm RequestedAlgorithm { get; init; }

    /// <summary>
    /// Gets how the decompressed value should be cached at runtime.
    /// </summary>
    public CompressionCacheMode CacheMode { get; init; }

    /// <summary>
    /// Gets a value indicating whether this member is covered by a <c>TarCompress</c> type.
    /// </summary>
    public bool IsCoveredByTar { get; init; }

    /// <summary>
    /// Gets the inline, resolved, or processed text. Null once binary transformation replaces it with bytes.
    /// </summary>
    public string? SourceValue
    {
        get => Value as string;
        init => Value = IsValid ? value : null;
    }

    /// <summary>
    /// Gets the processed binary data, if present, as a read-only view.
    /// </summary>
    public ReadOnlyMemory<byte> SourceBytes => Value is byte[] bytes ? bytes : default;

    /// <summary>
    /// Gets the file path for <see cref="CompressAttributeType.CompressFile"/> members; null for inline string members.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the file path is relative to the project root.
    /// </summary>
    public bool RelativeToProject { get; init; }

    /// <summary>
    /// Gets type of transformer assigned.
    /// </summary>
    public TransformerType TransformerType { get; init; }

    /// <summary>
    /// Gets the transformer selector, or null when no custom transformer is selected.
    /// </summary>
    public string? Transformer { get; init; }

    /// <summary>
    /// Gets the transformer member, or null when no custom transformer member is selected.
    /// </summary>
    public string? TransformerMember { get; init; }

    /// <summary>Gets explicit constructor arguments. Default means the property was omitted.</summary>
    public ImmutableArray<TransformerArgument> TransformerArguments { get; init; }

    /// <summary>Gets a value indicating whether processing may continue for this member.</summary>
    public bool IsValid { get; private init; } = true;

    /// <summary>
    /// Gets the compression attribute's syntax location, used during transformation and diagnostic reporting.
    /// </summary>
    public required LocationInfo Location { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an invalid null entry.
    /// </summary>
    public bool IsNull => MemberName is null;

    /// <inheritdoc/>
    Location ICompressionAttributeInfo.Location => Location.ToLocation();

    private object? Value { get; init; }

    /// <summary>
    /// Gets the absolute path of the file, if applicable. Null if <see cref="FilePath"/> is null.
    /// </summary>
    /// <param name="projectPath">The path to the project root.</param>
    /// <returns>The absolute path of the file, or null if not applicable.</returns>
    public string? GetAbsolutePath(string projectPath)
        => FilePath is null
        ? null
        : RelativeToProject
            ? Path.GetFullPath(Path.Combine(projectPath, FilePath))
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Location.FilePath) ?? string.Empty, FilePath));

    /// <summary>
    /// Gets processed binary data or encodes processed text as UTF-16 for compression or TAR assembly.
    /// </summary>
    /// <returns>The uncompressed bytes, or an empty array when no payload remains.</returns>
    public byte[] GetUncompressedData()
        => ReturnType switch
        {
            ReturnType.String => SourceValue is null ? [] : Encoding.Unicode.GetBytes(SourceValue),
            ReturnType.ByteArray => SourceBytes.ToArray(),
            _ => throw new InvalidOperationException("Unsupported members cannot supply compression data."),
        };

    /// <summary>
    /// Returns a copy with <see cref="SourceValue"/> replaced.
    /// Used to fill in the <see cref="SourceValue"/> for <see cref="CompressAttributeType.CompressFile"/>
    /// members once the file text is resolved.
    /// </summary>
    /// <param name="sourceValue">The new <see cref="SourceValue"/>.</param>
    /// <returns>A copy of this instance with <see cref="SourceValue"/> replaced.</returns>
    public CompressAttributeInfo WithSourceValue(string? sourceValue)
        => IsValid ? this with { Value = sourceValue } : this;

    /// <summary>
    /// Returns a copy containing an owned snapshot of processed bytes instead of source text.
    /// </summary>
    /// <param name="bytes">The processed binary value.</param>
    /// <returns>A copy containing only the binary payload.</returns>
    public CompressAttributeInfo WithSourceBytes(byte[] bytes)
        => IsValid ? this with { Value = bytes.ToArray() } : this;

    /// <summary>Invalidates the member and releases its payload without changing emission metadata.</summary>
    /// <returns>The invalid member, unchanged if already invalid.</returns>
    public CompressAttributeInfo Invalidate()
        => IsValid ? this with { IsValid = false, Value = null } : this;

    /// <summary>
    /// Indicates whether this instance is equal to another <see cref="CompressAttributeInfo"/> instance.
    ///
    /// Includes source locations; emission caching uses <see cref="EmissionComparer"/> instead.
    /// </summary>
    /// <param name="other">The other <see cref="CompressAttributeInfo"/> instance to compare.</param>
    /// <returns><c>true</c> if this instance is equal to <paramref name="other"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(CompressAttributeInfo other)
        => EqualsWithoutLocations(other)
        && Location.Equals(other.Location)
        && ContainingType.Location.Equals(other.ContainingType.Location);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CompressAttributeInfo other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => unchecked(((GetHashCodeWithoutLocations() * 397) ^ GetLocationHashCode(Location)) * 397)
            ^ GetLocationHashCode(ContainingType.Location);

    private static int GetLocationHashCode(LocationInfo location)
        => location.IsUnknown ? 0 : location.GetHashCode();

    private bool EqualsWithoutLocations(CompressAttributeInfo other)
        => ContainingType.Equals(other.ContainingType)
        && StringComparer.Ordinal.Equals(MemberName, other.MemberName)
        && MemberKind == other.MemberKind
        && StringComparer.Ordinal.Equals(Accessibility, other.Accessibility)
        && IsPartial == other.IsPartial
        && ReturnType == other.ReturnType
        && AttributeType == other.AttributeType
        && RequestedAlgorithm == other.RequestedAlgorithm
        && CacheMode == other.CacheMode
        && IsCoveredByTar == other.IsCoveredByTar
        && StringComparer.Ordinal.Equals(SourceValue, other.SourceValue)
        && (Value is byte[]) == (other.Value is byte[])
        && SourceBytes.Span.SequenceEqual(other.SourceBytes.Span)
        && StringComparer.Ordinal.Equals(FilePath, other.FilePath)
        && RelativeToProject == other.RelativeToProject
        && TransformerType == other.TransformerType
        && StringComparer.Ordinal.Equals(Transformer, other.Transformer)
        && StringComparer.Ordinal.Equals(TransformerMember, other.TransformerMember)
        && TransformerArguments.IsDefault == other.TransformerArguments.IsDefault
        && SequenceEquality.Equals(TransformerArguments, other.TransformerArguments)
        && IsValid == other.IsValid;

    private int GetHashCodeWithoutLocations()
    {
        unchecked
        {
            int hashCode = ContainingType.GetHashCode();
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(MemberName);
            hashCode = (hashCode * 397) ^ (int)MemberKind;
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Accessibility);
            hashCode = (hashCode * 397) ^ IsPartial.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)ReturnType;
            hashCode = (hashCode * 397) ^ (int)AttributeType;
            hashCode = (hashCode * 397) ^ (int)RequestedAlgorithm;
            hashCode = (hashCode * 397) ^ (int)CacheMode;
            hashCode = (hashCode * 397) ^ IsCoveredByTar.GetHashCode();
            hashCode = (hashCode * 397) ^ (SourceValue is null ? 0 : StringComparer.Ordinal.GetHashCode(SourceValue));
            hashCode = (hashCode * 397) ^ (Value is byte[]).GetHashCode();
            foreach (byte item in SourceBytes.Span)
            {
                hashCode = (hashCode * 397) ^ item;
            }

            hashCode = (hashCode * 397) ^ (FilePath is null ? 0 : StringComparer.Ordinal.GetHashCode(FilePath));
            hashCode = (hashCode * 397) ^ RelativeToProject.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)TransformerType;
            hashCode = (hashCode * 397) ^ (Transformer is null ? 0 : StringComparer.Ordinal.GetHashCode(Transformer));
            hashCode = (hashCode * 397) ^ (TransformerMember is null ? 0 : StringComparer.Ordinal.GetHashCode(TransformerMember));
            hashCode = (hashCode * 397) ^ TransformerArguments.IsDefault.GetHashCode();
            hashCode = (hashCode * 397) ^ SequenceEquality.GetHashCode(TransformerArguments);
            hashCode = (hashCode * 397) ^ IsValid.GetHashCode();
            return hashCode;
        }
    }

    private sealed class LocationComparer(bool includeFilePath) : IEqualityComparer<CompressAttributeInfo>
    {
        public bool Equals(CompressAttributeInfo x, CompressAttributeInfo y)
            => x.EqualsWithoutLocations(y)
            && (!includeFilePath || StringComparer.Ordinal.Equals(x.Location.FilePath, y.Location.FilePath));

        public int GetHashCode(CompressAttributeInfo obj)
            => includeFilePath
                ? unchecked((obj.GetHashCodeWithoutLocations() * 397) ^ (obj.Location.FilePath is null ? 0 : StringComparer.Ordinal.GetHashCode(obj.Location.FilePath)))
                : obj.GetHashCodeWithoutLocations();
    }
}
