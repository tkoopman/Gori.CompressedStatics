using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Provides information about a compression attribute, its target member, and its input text.
/// </summary>
public interface ICompressionAttributeInfo
{
    /// <summary>
    /// Gets the target member's accessibility as C# keywords, for example <c>public</c>.
    /// </summary>
    string Accessibility { get; }

    /// <summary>
    /// Gets the caching mode for the decompressed value at runtime.
    /// </summary>
    CompressionCacheMode CacheMode { get; }

    /// <summary>
    /// Gets the file path specified by a file compression attribute, or null for an inline value.
    /// </summary>
    string? FilePath { get; }

    /// <summary>
    /// Gets a value indicating whether the member is included in its containing type's TAR compression group.
    /// </summary>
    bool IsCoveredByTar { get; }

    /// <summary>
    /// Gets a value indicating whether the target member is declared with the <c>partial</c> modifier.
    /// </summary>
    bool IsPartial { get; }

    /// <summary>
    /// Gets the compression attribute's syntax location, used when reporting diagnostics.
    /// </summary>
    Location Location { get; }

    /// <summary>
    /// Gets the kind of member targeted by the attribute.
    /// </summary>
    MemberKind MemberKind { get; }

    /// <summary>
    /// Gets the name of the target member.
    /// </summary>
    string MemberName { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="FilePath"/> is relative to the project directory
    /// rather than the directory containing the attribute's source file.
    /// </summary>
    bool RelativeToProject { get; }

    /// <summary>
    /// Gets the requested compression algorithm, using the TAR group's algorithm when the member is covered by TAR compression.
    /// </summary>
    CompressionAlgorithm RequestedAlgorithm { get; }

    /// <summary>
    /// Gets the kind of compression attribute applied to the member.
    /// </summary>
    CompressAttributeType AttributeType { get; }

    /// <summary>
    /// Gets the target member's supported return type, or <see cref="ReturnType.Unsupported"/> if unsupported.
    /// </summary>
    ReturnType ReturnType { get; }

    /// <summary>
    /// Gets the inline value or resolved file text supplied for transformation,
    /// or null when no text is available, or already converted to bytes for compression.
    /// </summary>
    string? SourceValue { get; }
}
