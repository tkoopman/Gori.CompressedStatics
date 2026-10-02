using System.Collections.Immutable;

using Gori.Roslyn;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// An equatable description of a diagnostic to report, captured during the symbol-free transform
/// stage and materialized into a Roslyn <see cref="Diagnostic"/> at source-output time.
/// </summary>
internal class DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticInfo"/> class with a single message argument.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The source location.</param>
    /// <param name="messageArgument">The message format argument.</param>
    public DiagnosticInfo(DiagnosticDescriptor descriptor, LocationInfo location, string messageArgument)
        : this(descriptor, location, ImmutableArray.Create(messageArgument))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticInfo"/> class with message arguments.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The source location.</param>
    /// <param name="messageArguments">The message format arguments.</param>
    public DiagnosticInfo(DiagnosticDescriptor descriptor, LocationInfo location, params string[] messageArguments)
        : this(descriptor, location, messageArguments is null ? [] : ImmutableArray.Create(messageArguments))
    {
    }

    private DiagnosticInfo(DiagnosticDescriptor descriptor, LocationInfo location, ImmutableArray<string> messageArguments)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArguments = messageArguments.IsDefault ? [] : messageArguments;
    }

    /// <summary>
    /// Gets the descriptor to report.
    /// </summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the source location. Deliberately excluded from equality so line/column shifts from unrelated edits do not bust the incremental
    /// cache; a cached diagnostic keeps its earlier position only while all other data is unchanged.
    /// </summary>
    public LocationInfo Location { get; }

    /// <summary>
    /// Gets the message format arguments (typically the member or type name).
    /// </summary>
    public ImmutableArray<string> MessageArguments { get; }

    /// <summary>
    /// Materializes the Roslyn <see cref="Diagnostic"/>.
    /// </summary>
    /// <returns>The materialized <see cref="Diagnostic"/>.</returns>
    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(Descriptor, Location.ToLocation(), MessageArguments.ToArray());

    /// <summary>
    /// Determines whether the specified <see cref="DiagnosticInfo"/> is equal to the current <see cref="DiagnosticInfo"/>.
    /// Ignores the <see cref="Location"/> property, as it is not relevant to equality for caching purposes.
    /// </summary>
    /// <param name="other">The <see cref="DiagnosticInfo"/> to compare with the current <see cref="DiagnosticInfo"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="DiagnosticInfo"/> is equal to the current <see cref="DiagnosticInfo"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(DiagnosticInfo other)
        => StringComparer.Ordinal.Equals(Descriptor.Id, other.Descriptor.Id)
        && MessageArguments.SequenceEqual(other.MessageArguments, StringComparer.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DiagnosticInfo other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = StringComparer.Ordinal.GetHashCode(Descriptor.Id);
            foreach (string argument in MessageArguments)
            {
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(argument);
            }

            return hashCode;
        }
    }
}
