#pragma warning disable IDE0130

namespace Gori.Roslyn;

/// <summary>
/// A single declaration in a (possibly nested) type chain, capturing exactly what is needed to
/// reopen the type in generated source (for example <c>public static partial class Outer</c>).
/// </summary>
internal readonly struct TypeDeclaration(string keyword, string name) : IEquatable<TypeDeclaration>
{
    /// <summary>
    /// Gets the declaration keyword, for example <c>class</c> or <c>struct</c>.
    /// </summary>
    public string Keyword { get; } = keyword;

    /// <summary>
    /// Gets the type name including any type-parameter list, for example <c>Sample</c>.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">The other <see cref="TypeDeclaration"/> to compare with.</param>
    /// <returns><see langword="true"/> if the current object is equal to the other object; otherwise, <see langword="false"/>.</returns>
    public bool Equals(TypeDeclaration other)
        => StringComparer.Ordinal.Equals(Keyword, other.Keyword)
        && StringComparer.Ordinal.Equals(Name, other.Name);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TypeDeclaration other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = StringComparer.Ordinal.GetHashCode(Keyword);
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Name);
            return hashCode;
        }
    }
}
