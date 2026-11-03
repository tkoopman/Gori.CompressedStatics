using System.Collections.Immutable;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// An equatable description of a diagnostic to report, captured during the symbol-free transform
/// stage and materialized into a Roslyn <see cref="Diagnostic"/> at source-output time.
/// </summary>
public sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticInfo"/> class with no message arguments.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    public DiagnosticInfo(DiagnosticDescriptor descriptor)
        : this(descriptor, ImmutableArray<string>.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticInfo"/> class with a single message argument.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="messageArgument">The message format argument.</param>
    public DiagnosticInfo(DiagnosticDescriptor descriptor, string messageArgument)
        : this(descriptor, ImmutableArray.Create(messageArgument))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticInfo"/> class with message arguments.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="messageArguments">The message format arguments.</param>
    public DiagnosticInfo(DiagnosticDescriptor descriptor, params string[] messageArguments)
        : this(descriptor, messageArguments is null ? [] : ImmutableArray.Create(messageArguments))
    {
    }

    private DiagnosticInfo(DiagnosticDescriptor descriptor, ImmutableArray<string> messageArguments)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Severity = descriptor.DefaultSeverity;
        MessageArguments = messageArguments.IsDefault ? [] : messageArguments;
    }

    /// <summary>
    /// Gets the descriptor to report.
    /// </summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the effective severity. Defaults to the descriptor's severity.
    /// </summary>
    public DiagnosticSeverity Severity { get; init; }

    /// <summary>
    /// Gets the message format arguments (typically the member or type name).
    /// </summary>
    public ImmutableArray<string> MessageArguments { get; }

    /// <summary>Gets the additional diagnostic properties used by IDE code fixes.</summary>
    public ImmutableDictionary<string, string?> Properties { get; init; } = [];

    /// <summary>Gets an optional explicit reporting location. Excluded from value equality.</summary>
    public Location? Location { get; init; }

    /// <summary>Materializes the diagnostic at its explicit location, falling back to the supplied location.</summary>
    /// <param name="location">The fallback location, or null for Location.None.</param>
    /// <returns>The materialized <see cref="Diagnostic"/>.</returns>
    public Diagnostic ToDiagnostic(Location? location = null)
        => Diagnostic.Create(
            Descriptor,
            Location ?? location ?? Location.None,
            Severity,
            additionalLocations: null,
            properties: Properties,
            messageArgs: MessageArguments.ToArray());

    /// <summary>
    /// Determines whether the specified <see cref="DiagnosticInfo"/> is equal to the current <see cref="DiagnosticInfo"/>.
    /// Reporting locations do not participate in equality; keep location-sensitive reporting on a separate pipeline path.
    /// </summary>
    /// <param name="other">The <see cref="DiagnosticInfo"/> to compare with the current <see cref="DiagnosticInfo"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="DiagnosticInfo"/> is equal to the current <see cref="DiagnosticInfo"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(DiagnosticInfo? other)
        => other is not null
        && Descriptor.Equals(other.Descriptor)
        && Severity == other.Severity
        && SequenceEquality.Equals(MessageArguments, other.MessageArguments, StringComparer.Ordinal)
        && Properties.Count == other.Properties.Count
        && Properties.All(pair => other.Properties.TryGetValue(pair.Key, out string? value)
            && StringComparer.Ordinal.Equals(pair.Value, value));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DiagnosticInfo other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Descriptor.GetHashCode();
            hashCode = (hashCode * 397) ^ Severity.GetHashCode();
            hashCode = (hashCode * 397) ^ SequenceEquality.GetHashCode(MessageArguments, StringComparer.Ordinal);
            foreach (KeyValuePair<string, string?> pair in Properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(pair.Key);
                hashCode = (hashCode * 397) ^ (pair.Value is null ? 0 : StringComparer.Ordinal.GetHashCode(pair.Value));
            }

            return hashCode;
        }
    }
}
