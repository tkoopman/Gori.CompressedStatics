#pragma warning disable IDE0130

using System.Collections.Immutable;

namespace Gori.Roslyn;

/// <summary>
/// A fully symbol-free description of a containing type: enough to reopen it in generated code,
/// to build a stable hint name, and to key tar grouping. Holds no <c>ISymbol</c>.
/// </summary>
internal readonly struct EquatableTypeInfo(
    string @namespace,
    ImmutableArray<TypeDeclaration> declarations,
    string fullyQualifiedName,
    bool hasStaticConstructor,
    LocationInfo location) : IEquatable<EquatableTypeInfo>
{
    /// <summary>
    /// Gets the containing namespace, or an empty string when in the global namespace.
    /// </summary>
    public string Namespace { get; } = @namespace;

    /// <summary>
    /// Gets the enclosing type declarations from outermost to the target type (inclusive),
    /// used to open the nested partial declarations in order.
    /// </summary>
    public ImmutableArray<TypeDeclaration> Declarations { get; } = declarations.IsDefault ? [] : declarations;

    /// <summary>
    /// Gets the <c>global::</c>-qualified name of the target type, used as a stable identity for hint
    /// names and tar grouping.
    /// </summary>
    public string FullyQualifiedName { get; } = fullyQualifiedName;

    /// <summary>
    /// Gets a value indicating whether the user already declares a static constructor on this type.
    /// </summary>
    public bool HasStaticConstructor { get; } = hasStaticConstructor;

    /// <summary>
    /// Gets a source location for the type, used when reporting type-level diagnostics. Deliberately
    /// excluded from equality so that line/column shifts caused by unrelated edits do not bust the
    /// incremental cache. A stale cached location only survives while all equality-relevant data is unchanged.
    /// </summary>
    public LocationInfo Location { get; } = location;

    /// <summary>
    /// Indicates whether the current instance is equal to another <see cref="EquatableTypeInfo"/> instance.
    /// </summary>
    /// <param name="other">The other <see cref="EquatableTypeInfo"/> instance to compare with.</param>
    /// <returns><see langword="true"/> if the current instance is equal to the other instance; otherwise, <see langword="false"/>.</returns>
    public bool Equals(EquatableTypeInfo other)
        => StringComparer.Ordinal.Equals(Namespace, other.Namespace)
        && StringComparer.Ordinal.Equals(FullyQualifiedName, other.FullyQualifiedName)
        && HasStaticConstructor == other.HasStaticConstructor
        && SequenceEquality.Equals(Declarations, other.Declarations);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EquatableTypeInfo other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = StringComparer.Ordinal.GetHashCode(Namespace);
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(FullyQualifiedName);
            hashCode = (hashCode * 397) ^ HasStaticConstructor.GetHashCode();
            hashCode = (hashCode * 397) ^ SequenceEquality.GetHashCode(Declarations);
            return hashCode;
        }
    }
}
