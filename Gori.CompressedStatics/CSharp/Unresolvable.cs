namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Represents an unresolvable entity.
/// </summary>
public record Unresolvable : RewriteIssue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Unresolvable"/> class.
    /// </summary>
    /// <param name="type">The type of the unresolvable entity.</param>
    /// <param name="name">The name of the unresolvable entity.</param>
    public Unresolvable(UnresolvableType type, string name)
    {
        Type = type;
        Name = name;
    }

    /// <summary>
    /// Gets the type of unresolvable entry.
    /// </summary>
    public UnresolvableType Type { get; init; }

    /// <summary>
    /// Gets the name of the unresolvable entry.
    /// </summary>
    public string Name { get; init; }
}
