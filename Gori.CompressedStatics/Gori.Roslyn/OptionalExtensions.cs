#pragma warning disable IDE0130

using Microsoft.CodeAnalysis;

namespace Gori.Roslyn;

/// <summary>
/// Provides extension methods for the <see cref="Optional{T}"/> type to safely retrieve values as specific types.
/// </summary>
internal static class OptionalExtensions
{
    /// <summary>
    /// Tries to get the value of an <see cref="Optional{T}"/> as a string.
    /// </summary>
    /// <param name="value">The optional value to retrieve.</param>
    /// <param name="result">The resulting string if the value is present and of type string; otherwise, null.</param>
    /// <returns>True if the value is present and of type string; otherwise, false.</returns>
    public static bool TryGetValueAsString(this Optional<object?> value, out string result)
    {
        if (value.HasValue && value.Value is string stringValue)
        {
            result = stringValue;
            return true;
        }

        result = string.Empty;
        return false;
    }

    /// <summary>
    /// Tries to get the value of an <see cref="Optional{T}"/> as a boolean.
    /// </summary>
    /// <param name="value">The optional value to retrieve.</param>
    /// <param name="result">The resulting boolean if the value is present and of type boolean; otherwise, false.</param>
    /// <returns>True if the value is present and of type boolean; otherwise, false.</returns>
    public static bool TryGetValueAsBool(this Optional<object?> value, out bool result)
    {
        if (value.HasValue && value.Value is bool boolValue)
        {
            result = boolValue;
            return true;
        }

        result = default;
        return false;
    }
}
