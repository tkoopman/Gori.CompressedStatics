using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Helper class for c# rewriter.
/// </summary>
internal static class Helper
{
    /// <summary>
    /// Annotation used to mark resolved attribute types.
    /// </summary>
    internal const string ResolvedAttributeTypeAnnotation = "CSharpRewriter.ResolvedAttributeType";

    /// <summary>
    /// Symbol display format for fully qualified with miscellaneous options.
    /// </summary>
    internal static readonly SymbolDisplayFormat FullyQualifiedTypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// <see cref="FullyQualifiedTypeFormat"/> but no special types.
    /// </summary>
    internal static readonly SymbolDisplayFormat FullyQualifiedTypeFormatWithoutSpecialTypes = FullyQualifiedTypeFormat
        .WithMiscellaneousOptions(FullyQualifiedTypeFormat.MiscellaneousOptions & ~SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private enum NameSyntaxKind
    {
        Type,

        Method,
    }

    /// <summary>
    /// Validate an input type name and throw exception if invalid.
    /// </summary>
    /// <param name="typeName">Input type name.</param>
    /// <param name="paramName">Parameter name.</param>
    /// <param name="simpleOnly">If true, only allow simple named type paths without generic arguments, aliases, arrays, nullable modifiers, or pointers.</param>
    /// <returns>Validated type name with any trivia removed.</returns>
    /// <exception cref="ArgumentException">Invalid type name.</exception>
    public static string ThrowIfInvalidTypeName(string typeName, string paramName, bool simpleOnly)
        => TryValidateTypeName(typeName, simpleOnly, out string? validatedName, out string? error)
            ? validatedName
            : throw new ArgumentException(error, paramName);

    /// <summary>
    /// Validate an input method name and throw exception if invalid.
    /// </summary>
    /// <param name="typeName">Input method name.</param>
    /// <param name="paramName">Parameter name.</param>
    /// <returns>Validated method name with any trivia removed.</returns>
    /// <exception cref="ArgumentException">Invalid method name.</exception>
    public static string ThrowIfInvalidMethodName(string typeName, string paramName)
        => TryValidateMethodName(typeName, out string? validatedName, out string? error)
            ? validatedName
            : throw new ArgumentException(error, paramName);

    /// <summary>
    /// Validates a type name.
    /// </summary>
    /// <param name="name">The type name to validate.</param>
    /// <param name="simpleOnly">true to allow only simple type names; otherwise, false.</param>
    /// <param name="validatedName">When this method returns true, the validated type name; otherwise, null.</param>
    /// <param name="error">When validation fails, the reason validation failed; otherwise, null.</param>
    /// <returns>true if the type name is valid; otherwise, false.</returns>
    internal static bool TryValidateTypeName(string? name, bool simpleOnly, [NotNullWhen(true)] out string? validatedName, out string? error)
        => TryValidateName(name, NameSyntaxKind.Type, simpleOnly, out validatedName, out error);

    /// <summary>
    /// Validates a method name.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="validatedName">The validated method name when validation succeeds; otherwise, null.</param>
    /// <param name="error">An explanation of why validation failed, or null if validation succeeds.</param>
    /// <returns>true if the name is a valid method name; otherwise, false.</returns>
    internal static bool TryValidateMethodName(string? name, [NotNullWhen(true)] out string? validatedName, out string? error)
        => TryValidateName(name, NameSyntaxKind.Method, false, out validatedName, out error);

    private static bool IsNamedTypePath(TypeSyntax syntax)
        => syntax switch
        {
            IdentifierNameSyntax => true,
            QualifiedNameSyntax qualified =>
                IsNamedTypePath(qualified.Left)
                && qualified.Right is IdentifierNameSyntax,
            _ => false,
        };

    /// <summary>
    /// Checks if string is a valid type name format. Type doesn't need to exist.
    /// </summary>
    /// <param name="name">String to check.</param>
    /// <param name="typeSyntax">
    /// The parsed type syntax if valid.
    /// When invalid, maybe be non-null value with diagnostics.
    /// If it is null, the string was empty or whitespace.
    /// If no error level diagnostics, returned type swallowed extra trailing invalid characters.
    /// </param>
    /// <returns>True if string is valid type name format.</returns>
    private static bool IsValidNameSyntax(string? name, [NotNullWhen(true)] out TypeSyntax? typeSyntax)
    {
        if (name is null || name.IsWhiteSpace())
        {
            typeSyntax = null;
            return false;
        }

        name = name.Trim();

        // Parse the string as a TypeName syntax node
        typeSyntax = SyntaxFactory.ParseTypeName(name);

        // 1. Ensure it parsed cleanly with no compiler syntax errors
        if (typeSyntax.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return false;
        }

        // 2. Ensure the parser didn't swallow extra trailing invalid characters
        if (typeSyntax.FullSpan.Length != name.Length)
        {
            return false;
        }

        // Looks valid
        return true;
    }

    private static bool TryValidateName(
        string? typeName,
        NameSyntaxKind kind,
        bool simpleOnly,
        [NotNullWhen(true)] out string? validatedName,
        out string? error)
    {
        validatedName = null;
        error = null;
        if (!IsValidNameSyntax(typeName, out TypeSyntax? typeSyntax))
        {
            string[] diags = typeSyntax is null
                ? ["Name cannot be null, empty or whitespace."]
                : [.. typeSyntax.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.GetMessage(CultureInfo.InvariantCulture))];

            error = diags.Length == 0
                ? $"The name is not a valid C# {kind} name."
                : $"The name is not a valid C# {kind} name. {diags[0]}";
            return false;
        }

        if (simpleOnly && !IsNamedTypePath(typeSyntax))
        {
            error = "Type mappings require a dot-separated named type path without generic arguments, "
                + "aliases (including global::), arrays, nullable modifiers, or pointers.";
            return false;
        }

        if (kind == NameSyntaxKind.Type && typeSyntax.IsVar)
        {
            error = "Type name cannot be 'var'.";
            return false;
        }

        if (kind == NameSyntaxKind.Method && typeSyntax is not IdentifierNameSyntax)
        {
            error = "Invalid method name.";
            return false;
        }

        validatedName = string.Concat(typeSyntax.DescendantTokens()
            .Where(token => token.Parent is not TupleElementSyntax element || token != element.Identifier)
            .Select(token => token.Text));
        return true;
    }
}
