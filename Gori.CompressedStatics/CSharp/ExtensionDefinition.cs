using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gori.CompressedStatics.CSharp;

/// <summary>
/// Definition of extension method.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class ExtensionDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionDefinition"/> class.
    /// </summary>
    /// <param name="definition">The definition of the extension method.</param>
    /// <remarks>
    /// The definition must be in the format 'MethodName' or 'MethodName(paramType1, paramType2, ...)'.
    /// Use "..." at the end of the parameters list to allow extra parameters of any type.
    /// Parameter types must be resolvable types, or "*" to allow any type including unresolvable types in that position.
    /// The first parameter describes the receiver. Commas within generic types, array ranks, and tuples do not separate parameters.
    /// </remarks>
    public ExtensionDefinition(string definition)
        : this(Validate(definition))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionDefinition"/> class.
    /// </summary>
    /// <param name="name">The name of the extension method without type or generics.</param>
    /// <param name="parameters">List of parameter types. Use "*" to allow any type including unresolvable types in that position.</param>
    /// <param name="allowExtra">Indicates whether after <paramref name="parameters"/>, can extra parameters exist of any type.</param>
    public ExtensionDefinition(string name, IEnumerable<string> parameters, bool allowExtra)
    {
        Name = Helper.ThrowIfInvalidMethodName(name, nameof(name));
        Parameters = [.. parameters.Select(p => ValidateParameter(p, nameof(parameters)))];
        AllowExtra = allowExtra;
    }

    private ExtensionDefinition((string Name, ImmutableArray<string> Parameters, bool AllowExtra) validated)
    {
        Name = validated.Name;
        Parameters = validated.Parameters;
        AllowExtra = validated.AllowExtra;
    }

    /// <summary>
    /// Gets the name of the extension method without type or generics.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets list of parameter types. Use "*" to allow any type including unresolvable types in that position.
    /// </summary>
    /// <remarks>
    /// The order of the parameters matters and must match the order in the method definition.
    /// Types must be resolvable types, or "*" to allow any type including unresolvable types in that position.
    /// </remarks>
    public ImmutableArray<string> Parameters { get; }

    /// <summary>
    /// Gets a value indicating whether after <see cref="Parameters"/>, can extra parameters exist of any type.
    /// </summary>
    public bool AllowExtra { get; }

    /// <summary>Parses an extension definition without throwing for invalid configuration values.</summary>
    /// <param name="definition">The method name or method signature to parse.</param>
    /// <param name="extension">The definition on success, or null on failure.</param>
    /// <param name="issue">Validation details on failure, or null on success.</param>
    /// <returns>True when the definition is valid.</returns>
    public static bool TryCreate(
        string? definition,
        [NotNullWhen(true)] out ExtensionDefinition? extension,
        [NotNullWhen(false)] out InvalidConfiguration? issue)
    {
        if (!TryParse(definition, out (string Name, ImmutableArray<string> Parameters, bool AllowExtra) validated, out string? error))
        {
            extension = null;
            issue = new InvalidConfiguration(InvalidConfigurationReason.InvalidValue, definition)
            {
                Message = error,
                ArgumentName = nameof(definition),
                ArgumentValue = definition,
            };

            return false;
        }

        extension = new ExtensionDefinition(validated);
        issue = null;
        return true;
    }

    /// <summary>
    /// Checks equality to another ExtensionDefinition.
    /// </summary>
    /// <param name="other">The other ExtensionDefinition to compare to.</param>
    /// <returns>True if both are equal.</returns>
    public bool Equals(ExtensionDefinition? other)
        => other is not null
        && StringComparer.Ordinal.Equals(Name, other.Name)
        && AllowExtra == other.AllowExtra
        && SequenceEquality.Equals(Parameters, other.Parameters, StringComparer.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ExtensionDefinition other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + StringComparer.Ordinal.GetHashCode(Name);
            hash = (hash * 23) + AllowExtra.GetHashCode();
            hash = (hash * 23) + SequenceEquality.GetHashCode(Parameters, StringComparer.Ordinal);
            return hash;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        string parameters = Parameters.IsDefaultOrEmpty ? string.Empty : string.Join(", ", Parameters);
        if (AllowExtra)
        {
            parameters += (parameters.Length > 0 ? ", " : string.Empty) + "...";
        }

        return $"{Name}({parameters})";
    }

    private static string ValidateParameter(string parameter, string paramName)
        => TryValidateParameter(parameter, out string? validated, out string? error)
            ? validated
            : throw new ArgumentException(error, paramName);

    private static bool TryValidateParameter(string parameter, [NotNullWhen(true)] out string? validated, out string? error)
    {
        SyntaxToken[] tokens = [.. SyntaxFactory.ParseTokens(parameter).Where(token => !token.IsKind(SyntaxKind.EndOfFileToken))];
        if (tokens.Length == 1 && tokens[0].IsKind(SyntaxKind.AsteriskToken) && !tokens[0].ContainsDiagnostics)
        {
            validated = "*";
            error = null;
            return true;
        }

        return Helper.TryValidateTypeName(parameter, false, out validated, out error);
    }

    private static (string Name, ImmutableArray<string> Parameters, bool AllowExtra) Validate(string definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        // Validate the definition and throw if invalid, otherwise return the validated values.
        return TryParse(definition, out (string Name, ImmutableArray<string> Parameters, bool AllowExtra) validated, out string? error)
            ? validated
            : throw new ArgumentException(error, nameof(definition));
    }

    private static bool TryParse(
        string? definition,
        out (string Name, ImmutableArray<string> Parameters, bool AllowExtra) validated,
        out string? error)
    {
        validated = default;
        error = null;
        if (definition is null)
        {
            error = "An extension definition must be a non-null string.";
            return false;
        }

        const string invalidDefinition = "Definition must be in the format 'MethodName' or 'MethodName(param1, param2, ...)' with balanced delimiters.";
        SyntaxToken[] tokens = [.. SyntaxFactory.ParseTokens(definition).Where(token => !token.IsKind(SyntaxKind.EndOfFileToken))];
        int openIndex = Array.FindIndex(tokens, token => token.IsKind(SyntaxKind.OpenParenToken));
        string methodName = openIndex < 0 ? definition : definition[..tokens[openIndex].SpanStart];
        if (!Helper.TryValidateMethodName(methodName, out string? name, out error))
        {
            return false;
        }

        if (openIndex < 0)
        {
            validated = (name, default, true);
            return true;
        }

        if (!tokens[^1].IsKind(SyntaxKind.CloseParenToken))
        {
            error = invalidDefinition;
            return false;
        }

        var delimiters = new Stack<SyntaxKind>();
        var parts = new List<string>();
        int start = tokens[openIndex].Span.End;
        foreach (SyntaxToken token in tokens.Skip(openIndex + 1).Take(tokens.Length - openIndex - 2))
        {
            SyntaxKind? closing = null;
            int closingCount = 1;
            switch (token.Kind())
            {
                case SyntaxKind.LessThanToken:
                    delimiters.Push(SyntaxKind.GreaterThanToken);
                    break;
                case SyntaxKind.OpenBracketToken:
                    delimiters.Push(SyntaxKind.CloseBracketToken);
                    break;
                case SyntaxKind.OpenParenToken:
                    delimiters.Push(SyntaxKind.CloseParenToken);
                    break;
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.CloseParenToken:
                    closing = token.Kind();
                    break;
                case SyntaxKind.GreaterThanGreaterThanToken:
                case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
                    closing = SyntaxKind.GreaterThanToken;
                    closingCount = token.Text.Length;
                    break;
                case SyntaxKind.CommaToken when delimiters.Count == 0:
                    parts.Add(definition[start..token.SpanStart].Trim());
                    start = token.Span.End;
                    break;
            }

            if (closing is SyntaxKind kind)
            {
                for (int index = 0; index < closingCount; index++)
                {
                    if (delimiters.Count == 0 || delimiters.Pop() != kind)
                    {
                        error = invalidDefinition;
                        return false;
                    }
                }
            }
        }

        if (delimiters.Count != 0)
        {
            error = invalidDefinition;
            return false;
        }

        parts.Add(definition[start..tokens[^1].SpanStart].Trim());
        bool allowExtra = parts[^1] == "...";
        ImmutableArray<string>.Builder parameters = ImmutableArray.CreateBuilder<string>();
        foreach (string part in parts.Take(parts.Count - (allowExtra ? 1 : 0)))
        {
            if (!TryValidateParameter(part, out string? parameter, out error))
            {
                return false;
            }

            parameters.Add(parameter);
        }

        validated = (name, parameters.ToImmutable(), allowExtra);
        return true;
    }

    private string GetDebuggerDisplay() => ToString();
}
