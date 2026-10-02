namespace Gori.CompressedStatics.Generators;

/// <summary>
/// The kind of member the compression targets.
/// </summary>
internal enum MemberKind
{
    /// <summary>
    /// The member kind is unknown or unsupported.
    /// </summary>
    Unknown,

    /// <summary>
    /// The member is a class (with or without <c>partial</c>).
    /// </summary>
    Class,

    /// <summary>
    /// A property (with or without <c>partial</c>).
    /// </summary>
    Property,

    /// <summary>
    /// A method.
    /// </summary>
    Method,
}
