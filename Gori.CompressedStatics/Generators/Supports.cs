using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Stores compilation-wide information about what optional support is available in the current build.
/// </summary>
internal readonly struct Supports : IEquatable<Supports>
{
    /// <summary>
    /// Gets a value indicating whether modern Brotli support is available in the current compile-time environment.
    /// </summary>
    public required bool BrotliCompileTime { get; init; }

    /// <summary>
    /// Gets the level of Brotli support available in the current runtime environment.
    /// </summary>
    public required BrotliSupport BrotliRuntime { get; init; }

    /// <summary>
    /// Gets a value indicating whether Brotli is disabled.
    /// </summary>
    public bool DisableBrotli => BrotliRuntime is BrotliSupport.None || !BrotliCompileTime;

    /// <summary>
    /// Gets the project path for the current compilation, or an empty string if not set.
    /// </summary>
    public string ProjectPath { get => field ?? string.Empty; init; }

    /// <summary>
    /// Gets the supported features for the given compilation.
    /// </summary>
    /// <param name="compilation">The compilation to check for supported features.</param>
    /// <returns>A <see cref="Supports"/> instance representing the supported features.</returns>
    public static Supports GetSupported(Compilation compilation)
        => new Supports
        {
            BrotliCompileTime = Compressor.IsModernBrotliAvailable,
            BrotliRuntime = compilation.GetTypeByMetadataName(Const.BrotliStreamName21) is not null ? BrotliSupport.Modern
                   : compilation.GetTypeByMetadataName(Const.BrotliStreamName20) is not null ? BrotliSupport.Legacy
                   : BrotliSupport.None,
        };

    /// <summary>
    /// Creates a new instance of <see cref="Supports"/> with the specified project path.
    /// </summary>
    /// <param name="projectPath">The project path to set.</param>
    /// <returns>A new instance of <see cref="Supports"/> with the specified project path.</returns>
    public Supports WithProjectPath(string projectPath)
        => new Supports
        {
            BrotliCompileTime = BrotliCompileTime,
            BrotliRuntime = BrotliRuntime,
            ProjectPath = projectPath,
        };

    /// <inheritdoc/>
    public bool Equals(Supports other)
        => BrotliCompileTime == other.BrotliCompileTime
        && BrotliRuntime == other.BrotliRuntime
        && StringComparer.Ordinal.Equals(ProjectPath, other.ProjectPath);

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
        => obj is Supports other
        && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        unchecked
        {
            int hashCode = BrotliCompileTime.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)BrotliRuntime;
            hashCode = (hashCode * 397) ^ (ProjectPath != null ? StringComparer.Ordinal.GetHashCode(ProjectPath) : 0);
            return hashCode;
        }
    }
}
