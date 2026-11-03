using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Gori.Common.Roslyn;

namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Used to map unresolvable types to global type names.
/// </summary>
public class TypeMapping : IEquatable<TypeMapping>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeMapping"/> class.
    /// </summary>
    /// <param name="fullTypeName">
    /// The full type name, that the type should be written as.
    /// This excludes any generic type parameters, and global:: prefix.
    /// </param>
    /// <param name="replaces">The type name that this mapping replaces. Only required if different from the last segment of <paramref name="fullTypeName"/>.</param>
    public TypeMapping(string fullTypeName, string? replaces = null)
        : this(Validate(fullTypeName, replaces), default)
    {
    }

    private TypeMapping((string FullTypeName, string Replaces) validated, ImmutableArray<ExtensionDefinition> extensions)
    {
        FullTypeName = validated.FullTypeName;
        Replaces = validated.Replaces;
        ExtensionMethods = extensions;
    }

    /// <summary>
    /// Gets the full type name, that the type should be written as.
    /// This excludes any generic type parameters, and global:: prefix.
    /// </summary>
    public string FullTypeName { get; }

    /// <summary>
    /// Gets the type name that this mapping replaces. Only required if different from the last segment of <see cref="FullTypeName"/>.
    /// Unresolvable type names in the source code, must either exactly match or be potential subtype under exactly this value, excluding any generic type parameters.
    /// </summary>
    public string Replaces { get; }

    /// <summary>
    /// Gets list of extension methods this type contains and can be replaced
    /// if <see cref="CSharpRewriter.RewriteExtensionCalls"/> is enabled.
    /// </summary>
    public ImmutableArray<ExtensionDefinition> ExtensionMethods { get; init; }

    /// <summary>Creates a mapping without throwing for invalid configuration values.</summary>
    /// <param name="fullTypeName">The destination named type path.</param>
    /// <param name="replaces">The source path, or null to infer the last destination segment.</param>
    /// <param name="mapping">The mapping on success, or null on failure.</param>
    /// <param name="issue">Validation details on failure, or null on success.</param>
    /// <param name="extensions">Extension definitions associated with the mapping.</param>
    /// <returns>True when the mapping is valid.</returns>
    public static bool TryCreate(
        string? fullTypeName,
        string? replaces,
        [NotNullWhen(true)] out TypeMapping? mapping,
        [NotNullWhen(false)] out InvalidConfiguration? issue,
        ImmutableArray<ExtensionDefinition> extensions = default)
    {
        mapping = null;
        if (!TryValidate(fullTypeName, replaces, out (string FullTypeName, string Replaces) validated, out issue))
        {
            return false;
        }

        mapping = new TypeMapping(validated, extensions);
        return true;
    }

    /// <summary>
    /// Checks equality to another TypeMapping.
    /// </summary>
    /// <param name="other">The other TypeMapping to compare to.</param>
    /// <returns>True if both are equal.</returns>
    public bool Equals(TypeMapping? other)
        => other is not null
        && StringComparer.Ordinal.Equals(FullTypeName, other.FullTypeName)
        && StringComparer.Ordinal.Equals(Replaces, other.Replaces)
        && SequenceEquality.Equals(ExtensionMethods, other.ExtensionMethods);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TypeMapping other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = StringComparer.Ordinal.GetHashCode(FullTypeName);
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Replaces);
            hashCode = (hashCode * 397) ^ SequenceEquality.GetHashCode(ExtensionMethods);

            return hashCode;
        }
    }

    private static (string FullTypeName, string Replaces) Validate(string fullTypeName, string? replaces)
        => fullTypeName is not null
            ? TryValidate(fullTypeName, replaces, out (string FullTypeName, string Replaces) validated, out InvalidConfiguration? issue)
                ? validated
                : throw new ArgumentException(issue.Message, issue.ArgumentName)
            : throw new ArgumentNullException(nameof(fullTypeName));

    private static bool TryValidate(
        string? fullTypeName,
        string? replaces,
        out (string FullTypeName, string Replaces) validated,
        [NotNullWhen(false)] out InvalidConfiguration? issue)
    {
        validated = default;
        issue = null;
        string? argumentName;
        string? argumentValue;
        string? source = null;

        if (!Helper.TryValidateTypeName(fullTypeName, true, out string? destination, out string? error))
        {
            argumentName = nameof(fullTypeName);
            argumentValue = fullTypeName;

            return RaiseError(error, argumentName, argumentValue, out issue);
        }

        if (replaces is not null && !Helper.TryValidateTypeName(replaces, true, out source, out error))
        {
            argumentName = nameof(replaces);
            argumentValue = replaces;
            return RaiseError(error, argumentName, argumentValue, out issue);
        }

        validated = (destination, source ?? destination.Split('.')[^1]);
        return true;

        static bool RaiseError(string? error, string? argumentName, string? argumentValue, out InvalidConfiguration issue)
        {
            issue = new InvalidConfiguration(InvalidConfigurationReason.InvalidValue, argumentValue)
            {
                Message = error,
                ArgumentName = argumentName,
                ArgumentValue = argumentValue,
            };

            return false;
        }
    }
}
