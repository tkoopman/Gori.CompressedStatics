#pragma warning disable IDE0130

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Gori.Roslyn;

/// <summary>
/// An equatable, symbol-free snapshot of a source <see cref="Location"/> so diagnostics can flow
/// through the incremental pipeline without breaking caching.
/// </summary>
internal readonly struct LocationInfo : IEquatable<LocationInfo>
{
    /// <summary>
    /// A sentinel value representing an unknown or unavailable location.
    /// </summary>
    public static readonly LocationInfo Unknown = new LocationInfo(string.Empty, default, default);

    private LocationInfo(string filePath, TextSpan textSpan, LinePositionSpan lineSpan)
    {
        FilePath = filePath;
        TextSpan = textSpan;
        LineSpan = lineSpan;
    }

    /// <summary>
    /// Gets the source file path.
    /// File must be added to the compilation as an additional text for the location to be valid.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the character span within the file.
    /// </summary>
    public TextSpan TextSpan { get; }

    /// <summary>
    /// Gets the line/column span within the file.
    /// </summary>
    public LinePositionSpan LineSpan { get; }

    /// <summary>
    /// Gets a value indicating whether this location is unknown or unavailable.
    /// </summary>
    public bool IsUnknown => string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// Captures a symbol's first declaration location, or <see cref="Unknown"/> when unavailable.
    /// </summary>
    /// <param name="symbol">The symbol to capture the location from.</param>
    /// <returns>A <see cref="LocationInfo"/> representing the symbol's location, or <see cref="Unknown"/> if unavailable.</returns>
    public static LocationInfo From(ISymbol symbol)
    {
        Location? location = symbol.Locations.Length > 0 ? symbol.Locations[0] : null;
        return From(location);
    }

    /// <summary>
    /// Captures a <see cref="Location"/> in source, or <see cref="Unknown"/> when it is not in a source tree.
    /// </summary>
    /// <param name="location">The location to capture.</param>
    /// <returns>A <see cref="LocationInfo"/> representing the location, or <see cref="Unknown"/> if unavailable.</returns>
    public static LocationInfo From(Location? location)
        => location is { Kind: LocationKind.SourceFile } && location.SourceTree is not null
            ? new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span)
            : Unknown;

    /// <summary>
    /// Rehydrates a Roslyn <see cref="Location"/> for diagnostic reporting.
    /// </summary>
    /// <returns>A <see cref="Location"/> representing this location info.</returns>
    public readonly Location ToLocation() => IsUnknown ? Location.None : Location.Create(FilePath, TextSpan, LineSpan);

    /// <summary>
    /// Indicates whether this instance is equal to another <see cref="LocationInfo"/> instance.
    /// </summary>
    /// <param name="other">The other <see cref="LocationInfo"/> instance to compare with.</param>
    /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(LocationInfo other)
        => StringComparer.Ordinal.Equals(FilePath, other.FilePath)
        && TextSpan.Equals(other.TextSpan)
        && LineSpan.Equals(other.LineSpan);

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is LocationInfo other && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        unchecked
        {
            int hashCode = StringComparer.Ordinal.GetHashCode(FilePath);
            hashCode = (hashCode * 397) ^ TextSpan.GetHashCode();
            hashCode = (hashCode * 397) ^ LineSpan.GetHashCode();
            return hashCode;
        }
    }
}
