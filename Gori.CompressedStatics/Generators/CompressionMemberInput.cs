using System.Collections.Immutable;
using System.Text;

using Gori.Roslyn;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// A symbol-free description of a single member to compress, for Roslyn value caching.
///
/// Stores the raw text content of the member, but leaves the actual bytes to be derived on demand at emit time
/// via <see cref="GetUncompressedData"/>.
///
/// While it stores location information for diagnostic reporting, the location is deliberately excluded from equality and
/// hash code so that line/column shifts from unrelated edits do not bust the incremental cache and force re-compression
/// of unchanged payloads.
/// </summary>
internal readonly struct CompressionMemberInput : IEquatable<CompressionMemberInput>
{
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
    public required CompressionSource Source { get; init; }

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
    /// Gets the raw text content of the member: for <see cref="CompressionSource.String"/> the inline
    /// attribute value; for <see cref="CompressionSource.File"/> the resolved additional-file text
    /// (null until resolved). Its bytes are derived on demand at emit time via
    /// <see cref="GetUncompressedData"/> so the payload is never retained in the incremental cache.
    /// </summary>
    public string? SourceValue { get; init; }

    /// <summary>
    /// Gets additional file's path for <see cref="CompressionSource.File"/> members; null for inline string members.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the file path is relative to the project root.
    /// </summary>
    public bool RelativeToProject { get; init; }

    /// <summary>
    /// Gets the member's source location, used when reporting diagnostics. Deliberately
    /// excluded from equality so line/column shifts from unrelated edits do not bust the incremental
    /// cache and force re-compression of unchanged payloads.
    /// </summary>
    public required LocationInfo Location { get; init; }

    /// <summary>
    /// Gets an optional diagnostic to report for this member (for example an invalid base64 value).
    /// </summary>
    public ImmutableArray<DiagnosticInfo> Diagnostics { get; init; }

    /// <summary>
    /// Gets the absolute path of the additional file, if applicable. Null if <see cref="FilePath"/> is null.
    /// </summary>
    /// <param name="projectPath">The path to the project root.</param>
    /// <returns>The absolute path of the additional file, or null if not applicable.</returns>
    public string? GetAbsolutePath(string projectPath)
        => FilePath is null
        ? null
        : RelativeToProject
            ? Path.GetFullPath(Path.Combine(projectPath, FilePath))
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Location.FilePath) ?? string.Empty, FilePath));

    /// <summary>
    /// Derives the uncompressed bytes from <see cref="SourceValue"/> on demand, so payloads are only
    /// materialized when actually needed (compression or tar assembly) and never retained in the
    /// incremental cache. A <see cref="ReturnType.String"/> member is UTF-16 encoded; a
    /// <see cref="ReturnType.ByteArray"/> member decodes its base64 text, reporting
    /// <paramref name="diagnostic"/> when it is invalid.
    /// </summary>
    /// <param name="diagnostic">Outputs a diagnostic if the uncompressed data is invalid; otherwise, null.</param>
    /// <returns>The uncompressed bytes, or an empty array if <see cref="SourceValue"/> is null.</returns>
    public byte[] GetUncompressedData(out DiagnosticInfo? diagnostic)
    {
        diagnostic = null;

        if (SourceValue is null)
        {
            return [];
        }

        if (ReturnType == ReturnType.String)
        {
            return Encoding.Unicode.GetBytes(SourceValue);
        }

        if (ReturnType == ReturnType.ByteArray)
        {
            if (Helper.TryConvertFromBase64(SourceValue, out byte[] bytes))
            {
                return bytes;
            }

            diagnostic = new DiagnosticInfo(DiagnosticDescriptors.InvalidBase64String, Location, MemberName);
        }

        return [];
    }

    /// <summary>
    /// Returns a copy with <see cref="SourceValue"/> replaced.
    /// Used to fill in the <see cref="SourceValue"/> for <see cref="CompressionSource.File"/>
    /// members once the additional text is resolved.
    /// </summary>
    /// <param name="sourceValue">The new <see cref="SourceValue"/>.</param>
    /// <returns>A copy of this instance with <see cref="SourceValue"/> replaced.</returns>
    public CompressionMemberInput WithSourceValue(string? sourceValue)
        => new()
        {
            ContainingType = ContainingType,
            MemberName = MemberName,
            MemberKind = MemberKind,
            Accessibility = Accessibility,
            IsPartial = IsPartial,
            ReturnType = ReturnType,
            Source = Source,
            RequestedAlgorithm = RequestedAlgorithm,
            CacheMode = CacheMode,
            IsCoveredByTar = IsCoveredByTar,
            SourceValue = sourceValue,
            FilePath = FilePath,
            RelativeToProject = RelativeToProject,
            Location = Location,
            Diagnostics = Diagnostics,
        };

    /// <summary>
    /// Returns a copy with <paramref name="diagnostic"/> added to <see cref="Diagnostics"/>.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    /// <returns>A copy of this instance with <paramref name="diagnostic"/> added to <see cref="Diagnostics"/>.</returns>
    public CompressionMemberInput AddDiagnostic(DiagnosticInfo diagnostic)
        => new()
        {
            ContainingType = ContainingType,
            MemberName = MemberName,
            MemberKind = MemberKind,
            Accessibility = Accessibility,
            IsPartial = IsPartial,
            ReturnType = ReturnType,
            Source = Source,
            RequestedAlgorithm = RequestedAlgorithm,
            CacheMode = CacheMode,
            IsCoveredByTar = IsCoveredByTar,
            SourceValue = SourceValue,
            FilePath = FilePath,
            RelativeToProject = RelativeToProject,
            Location = Location,
            Diagnostics = Diagnostics.Add(diagnostic),
        };

    /// <summary>
    /// Indicates whether this instance is equal to another <see cref="CompressionMemberInput"/> instance.
    ///
    /// As used for incremental caching, equality deliberately ignores <see cref="Location"/> so that line/column
    /// shifts from unrelated edits do not bust the cache and force re-compression of unchanged payloads.
    /// </summary>
    /// <param name="other">The other <see cref="CompressionMemberInput"/> instance to compare.</param>
    /// <returns><c>true</c> if this instance is equal to <paramref name="other"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(CompressionMemberInput other)
        => ContainingType.Equals(other.ContainingType)
        && StringComparer.Ordinal.Equals(MemberName, other.MemberName)
        && MemberKind == other.MemberKind
        && StringComparer.Ordinal.Equals(Accessibility, other.Accessibility)
        && IsPartial == other.IsPartial
        && ReturnType == other.ReturnType
        && Source == other.Source
        && RequestedAlgorithm == other.RequestedAlgorithm
        && CacheMode == other.CacheMode
        && IsCoveredByTar == other.IsCoveredByTar
        && StringComparer.Ordinal.Equals(SourceValue, other.SourceValue)
        && StringComparer.Ordinal.Equals(FilePath, other.FilePath)
        && RelativeToProject == other.RelativeToProject
        && SequenceEquality.Equals(Diagnostics, other.Diagnostics);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CompressionMemberInput other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = ContainingType.GetHashCode();
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(MemberName);
            hashCode = (hashCode * 397) ^ (int)MemberKind;
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Accessibility);
            hashCode = (hashCode * 397) ^ IsPartial.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)ReturnType;
            hashCode = (hashCode * 397) ^ (int)Source;
            hashCode = (hashCode * 397) ^ (int)RequestedAlgorithm;
            hashCode = (hashCode * 397) ^ (int)CacheMode;
            hashCode = (hashCode * 397) ^ IsCoveredByTar.GetHashCode();
            hashCode = (hashCode * 397) ^ (SourceValue is null ? 0 : StringComparer.Ordinal.GetHashCode(SourceValue));
            hashCode = (hashCode * 397) ^ (FilePath is null ? 0 : StringComparer.Ordinal.GetHashCode(FilePath));
            hashCode = (hashCode * 397) ^ RelativeToProject.GetHashCode();
            hashCode = (hashCode * 397) ^ SequenceEquality.GetHashCode(Diagnostics);
            return hashCode;
        }
    }
}
