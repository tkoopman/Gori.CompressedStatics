using System.Collections.Immutable;

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

    /// <summary>
    /// Materializes the Roslyn <see cref="Diagnostic"/> at the owning attribute's location.
    /// </summary>
    /// <param name="location">The owning attribute's location.</param>
    /// <returns>The materialized <see cref="Diagnostic"/>.</returns>
    public Diagnostic ToDiagnostic(Location location)
        => Diagnostic.Create(
            Descriptor,
            location,
            Severity,
            additionalLocations: null,
            properties: null,
            messageArgs: MessageArguments.ToArray());

    /// <summary>
    /// Determines whether the specified <see cref="DiagnosticInfo"/> is equal to the current <see cref="DiagnosticInfo"/>.
    /// The primary reporting location is supplied separately and does not participate in equality.
    /// </summary>
    /// <param name="other">The <see cref="DiagnosticInfo"/> to compare with the current <see cref="DiagnosticInfo"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="DiagnosticInfo"/> is equal to the current <see cref="DiagnosticInfo"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(DiagnosticInfo? other)
        => other is not null
        && Descriptor.Equals(other.Descriptor)
        && Severity == other.Severity
        && MessageArguments.SequenceEqual(other.MessageArguments, StringComparer.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DiagnosticInfo other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Descriptor.GetHashCode();
            hashCode = (hashCode * 397) ^ Severity.GetHashCode();
            foreach (string argument in MessageArguments)
            {
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(argument);
            }

            return hashCode;
        }
    }
}
