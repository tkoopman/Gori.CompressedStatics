using System.Collections.Immutable;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Carries processing output and diagnostics until their value and reporting pipeline branches separate.
/// Compare Input, not this location-sensitive envelope, in downstream value pipelines.
/// </summary>
/// <typeparam name="T">The processed value type.</typeparam>
/// <param name="input">The processed value.</param>
/// <param name="diagnostics">The reporting diagnostics.</param>
/// <remarks>Initializes a new instance of the <see cref="ProcessingResult{T}"/> struct with multiple diagnostics.</remarks>
internal readonly struct ProcessingResult<T>(T input, ImmutableArray<DiagnosticInfo> diagnostics)
{
    /// <summary>Initializes a new instance of the <see cref="ProcessingResult{T}"/> struct with an optional diagnostic.</summary>
    /// <param name="input">The processed value.</param>
    /// <param name="diagnostic">The optional diagnostic description.</param>
    public ProcessingResult(T input, DiagnosticInfo? diagnostic = null)
        : this(input, diagnostic is null ? [] : [diagnostic])
    {
    }

    /// <summary>Gets the processed value, independently comparable from diagnostics.</summary>
    public T Input { get; } = input;

    /// <summary>Gets the diagnostics, including any explicit reporting locations.</summary>
    public ImmutableArray<DiagnosticInfo> Diagnostics { get; } = diagnostics.IsDefault ? [] : diagnostics;
}
