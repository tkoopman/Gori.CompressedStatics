using System;
using System.Diagnostics;

namespace Gori.CompressedStatics;

/// <summary>
/// Configures a mapping for unresolved or ambiguous type references.
/// </summary>
/// <param name="fullTypeName">The destination named type path, without generic arguments or a global:: prefix.</param>
/// <param name="replaces">The source named type path. Null infers the last segment of the destination.</param>
/// <remarks>
/// The transformer loads assembly-wide rules from the host compilation.
/// Declare configuration in a separate compiled file, not in the source being rewritten.
/// </remarks>
[Conditional("Gori_CompressedStatics_EmitAttributes")]
[AttributeUsage(
    AttributeTargets.Assembly,
    AllowMultiple = true,
    Inherited = false)]
public sealed class CSharpRewriterTypeMappingAttribute(string fullTypeName, string? replaces = null) : Attribute
{
    /// <summary>
    /// Gets the destination named type path, without generic arguments or a global:: prefix.
    /// </summary>
    public string FullTypeName { get; } = fullTypeName;

    /// <summary>
    /// Gets the source named type path, or null to infer it from <see cref="FullTypeName"/>.
    /// </summary>
    public string? Replaces { get; } = replaces;

    /// <summary>
    /// Gets or sets extension method definitions declared by the destination type.
    /// Only used when extension-call rewriting is enabled.
    /// </summary>
    /// <remarks>
    /// Each entry is a method name alone to allow any parameters;
    /// otherwise the first parameter is the receiver, "*" matches any type, and a final "..." allows extra parameters.
    /// For example, "Method(*, string, ...)" matches a receiver, a string argument, and any further arguments.
    /// </remarks>
    public string[] ExtensionMethods { get; set; } = [];
}
