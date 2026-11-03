using System.Collections.Immutable;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Immutable, explicitly supplied rewriter rules.
/// </summary>
public sealed class RewriterConfiguration : IEquatable<RewriterConfiguration>
{
    private RewriterConfiguration(
        ImmutableArray<Ignore> ignores,
        ImmutableDictionary<string, TypeMapping> typeMappings)
    {
        Ignores = ignores;
        TypeMappings = typeMappings;
    }

    /// <summary>Gets configuration with no rules.</summary>
    public static RewriterConfiguration Empty { get; } = new(
        [],
        ImmutableDictionary.Create<string, TypeMapping>(StringComparer.Ordinal));

    /// <summary>Gets the distinct diagnostic ignores.</summary>
    public ImmutableArray<Ignore> Ignores { get; }

    /// <summary>Gets the non-conflicting type mappings.</summary>
    public ImmutableDictionary<string, TypeMapping> TypeMappings { get; }

    /// <summary>Creates configuration from explicitly constructed rules.</summary>
    /// <param name="ignores">Diagnostic ignores. A default array means no ignores.</param>
    /// <param name="typeMappings">Type mappings. A default array means no mappings.</param>
    /// <param name="issues">Conflicting mapping issues.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration, or <see cref="Empty"/> when no valid rules remain.</returns>
    public static RewriterConfiguration Create(
        ImmutableArray<Ignore> ignores,
        ImmutableArray<TypeMapping> typeMappings,
        out IEnumerable<RewriteIssue> issues,
        CancellationToken cancellationToken)
        => CreateWithLocations(
            ignores,
            typeMappings.IsDefaultOrEmpty ? [] : typeMappings.Select(mapping => (mapping, (Location?)null)),
            out issues,
            cancellationToken);

    /// <inheritdoc/>
    public bool Equals(RewriterConfiguration? other)
        => other is not null
        && SequenceEquality.Equals(Ignores, other.Ignores)
        && TypeMappings.Count == other.TypeMappings.Count
        && TypeMappings.All(entry => other.TypeMappings.TryGetValue(entry.Key, out TypeMapping? mapping) && entry.Value.Equals(mapping));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RewriterConfiguration other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = SequenceEquality.GetHashCode(Ignores);
            foreach (KeyValuePair<string, TypeMapping> entry in TypeMappings.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                hashCode = (hashCode * 397) ^ entry.Value.GetHashCode();
            }

            return hashCode;
        }
    }

    /// <summary>Finds matching diagnostic ignores.</summary>
    /// <param name="diagnosticID">The compiler diagnostic ID.</param>
    /// <param name="message">The invariant compiler diagnostic message.</param>
    /// <returns>Ignores matching both the ID and optional case-sensitive message filter.</returns>
    public IEnumerable<Ignore> GetMatchingIgnores(string diagnosticID, string message)
    {
        if (diagnosticID is null)
        {
            throw new ArgumentNullException(nameof(diagnosticID));
        }

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        // Return ignores that match both the diagnostic ID and optional message filter.
        return Ignores.Where(ignore => ignore.DiagnosticID == diagnosticID
            && (ignore.MessageContains is null || message.Contains(ignore.MessageContains, StringComparison.Ordinal)));
    }

    /// <summary>Finds the longest matching replacement path.</summary>
    /// <param name="nameSegments">Reference identifiers, without generic arguments.</param>
    /// <param name="attribute">Whether the final reference segment may omit the Attribute suffix.</param>
    /// <returns>The matching mapping, or null when none applies.</returns>
    public TypeMapping? FindTypeMapping(IReadOnlyList<string> nameSegments, bool attribute = false)
    {
        if (nameSegments is null)
        {
            throw new ArgumentNullException(nameof(nameSegments));
        }

        // Return the longest matching mapping, or null when none applies.
        return TypeMappings.Values
            .Where(mapping => Matches(mapping.Replaces, nameSegments, attribute))
            .OrderByDescending(mapping => mapping.Replaces.Split('.').Length)
            .ThenBy(mapping => mapping.Replaces, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>Creates rules with optional source locations for conflict diagnostics.</summary>
    /// <param name="ignores">The diagnostic ignores.</param>
    /// <param name="typeMappings">Mappings and their original source locations.</param>
    /// <param name="issues">Conflicting mapping issues.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration, or <see cref="Empty"/> when no valid rules remain.</returns>
    internal static RewriterConfiguration CreateWithLocations(
        ImmutableArray<Ignore> ignores,
        IEnumerable<(TypeMapping Mapping, Location? Location)> typeMappings,
        out IEnumerable<RewriteIssue> issues,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<RewriteIssue>? issueList = null;
        ImmutableDictionary<string, TypeMapping>.Builder? mappings = null;
        ImmutableHashSet<string>.Builder? conflicts = null;
        foreach ((TypeMapping mapping, Location? location) in typeMappings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (mapping is null)
            {
                throw new ArgumentException("Mappings must not contain null entries.", nameof(typeMappings));
            }

            AddMapping(mapping, location, ref mappings, ref conflicts, ref issueList);
        }

        if (!ignores.IsDefaultOrEmpty && ignores.Any(ignore => ignore is null))
        {
            throw new ArgumentException("Ignores must not contain null entries.", nameof(ignores));
        }

        issues = issueList is null ? [] : issueList.ToImmutableArray();
        if (ignores.IsDefaultOrEmpty && (mappings is null || mappings.Count == 0))
        {
            return Empty;
        }

        // Return the configuration with distinct ignores and non-conflicting mappings.
        return new RewriterConfiguration(
            ignores.IsDefaultOrEmpty ? Empty.Ignores : [.. ignores.Distinct()],
            mappings?.ToImmutable() ?? Empty.TypeMappings);
    }

    private static bool Matches(string replaces, IReadOnlyList<string> nameSegments, bool attribute)
    {
        string[] segments = replaces.Split('.');
        return segments.Length <= nameSegments.Count
            && segments.Select((segment, index) => StringComparer.Ordinal.Equals(segment, nameSegments[index])
                || (attribute && index == nameSegments.Count - 1 && segment == nameSegments[index] + "Attribute")).All(match => match);
    }

    private static void AddMapping(
        TypeMapping mapping,
        Location? location,
        ref ImmutableDictionary<string, TypeMapping>.Builder? mappings,
        ref ImmutableHashSet<string>.Builder? conflicts,
        ref List<RewriteIssue>? issues)
    {
        if (conflicts?.Contains(mapping.Replaces) == true
            || (mappings is not null && mappings.TryGetValue(mapping.Replaces, out TypeMapping? existing) && !existing.Equals(mapping)))
        {
            _ = mappings?.Remove(mapping.Replaces);
            conflicts ??= ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
            _ = conflicts.Add(mapping.Replaces);
            (issues ??= []).Add(new InvalidConfiguration(InvalidConfigurationReason.ConflictingRules, mapping.Replaces)
            {
                Location = location,
            });
            return;
        }

        mappings ??= ImmutableDictionary.CreateBuilder<string, TypeMapping>(StringComparer.Ordinal);
        mappings[mapping.Replaces] = mapping;
    }
}
