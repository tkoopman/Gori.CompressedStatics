#pragma warning disable IDE0130

using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace Gori.Roslyn;

/// <summary>
/// Extension methods for <see cref="TypedConstant"/> to simplify type checking and value extraction.
/// </summary>
internal static class TypedConstantExtensions
{
    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a boolean and extracts its value if so.
    /// </summary>
    /// <param name="constant">The <see cref="TypedConstant"/> to check.</param>
    /// <param name="value">
    /// When this method returns true, contains the value of the <see cref="TypedConstant"/>.
    /// </param>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a boolean; otherwise, <see langword="false"/>.</returns>
    public static bool IsBool(this TypedConstant constant, out bool value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is bool boolValue)
        {
            value = boolValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a byte and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a byte; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsByte(this TypedConstant constant, out byte value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is byte byteValue)
        {
            value = byteValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a char and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a char; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsChar(this TypedConstant constant, out char value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is char charValue)
        {
            value = charValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a decimal and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a decimal; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsDecimal(this TypedConstant constant, out decimal value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is decimal decimalValue)
        {
            value = decimalValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a double and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a double; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsDouble(this TypedConstant constant, out double value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is double doubleValue)
        {
            value = doubleValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a enum and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a enum; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsEnum<TEnum>(this TypedConstant constant, out TEnum value, bool ignoreEnumType = false)
        where TEnum : struct, Enum
    {
        if (constant.Kind == TypedConstantKind.Enum && (ignoreEnumType || constant.Type?.ToDisplayString() == typeof(TEnum).FullName))
        {
            switch (constant.Value)
            {
                case int intValue:
                    value = (TEnum)Enum.ToObject(typeof(TEnum), intValue);
                    return true;

                case byte byteValue:
                    value = (TEnum)Enum.ToObject(typeof(TEnum), byteValue);
                    return true;

                case long longValue:
                    value = (TEnum)Enum.ToObject(typeof(TEnum), longValue);
                    return true;

                case sbyte sbyteValue:
                    value = (TEnum)Enum.ToObject(typeof(TEnum), sbyteValue);
                    return true;

                case short shortValue:
                    value = (TEnum)Enum.ToObject(typeof(TEnum), shortValue);
                    return true;

                case uint uintValue:
                    value = (TEnum)Enum.ToObject(typeof(TEnum), uintValue);
                    return true;

                case ulong ulongValue:
                    value = (TEnum)Enum.ToObject(typeof(TEnum), ulongValue);
                    return true;

                case ushort ushortValue:
                    value = (TEnum)Enum.ToObject(typeof(TEnum), ushortValue);
                    return true;

                default:
                    break;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a float and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a float; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsFloat(this TypedConstant constant, out float value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is float floatValue)
        {
            value = floatValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a int and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a int; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsInt(this TypedConstant constant, out int value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is int intValue)
        {
            value = intValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a long and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a long; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsLong(this TypedConstant constant, out long value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is long longValue)
        {
            value = longValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a sbyte and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a sbyte; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsSByte(this TypedConstant constant, out sbyte value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is sbyte sbyteValue)
        {
            value = sbyteValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a short and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a short; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsShort(this TypedConstant constant, out short value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is short shortValue)
        {
            value = shortValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a string and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a string; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsString(this TypedConstant constant, [NotNullWhen(true)] out string? value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is string stringValue)
        {
            value = stringValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a uint and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a uint; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsUInt(this TypedConstant constant, out uint value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is uint uintValue)
        {
            value = uintValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a ulong and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a ulong; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsULong(this TypedConstant constant, out ulong value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is ulong ulongValue)
        {
            value = ulongValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="TypedConstant"/> is a ushort and extracts its value if so.
    /// </summary>
    /// <returns><see langword="true"/> if the <see cref="TypedConstant"/> is a ushort; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IsBool(TypedConstant, out bool)"/>
    public static bool IsUShort(this TypedConstant constant, out ushort value)
    {
        if (constant.Kind == TypedConstantKind.Primitive && constant.Value is ushort ushortValue)
        {
            value = ushortValue;
            return true;
        }

        value = default;
        return false;
    }
}
