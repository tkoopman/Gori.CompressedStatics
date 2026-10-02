#pragma warning disable IDE0130

using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace Gori.Roslyn;

/// <summary>
/// Provides extension methods for working with <see cref="AttributeData"/> in Roslyn.
/// </summary>
internal static class AttributeDataExtensions
{
    /// <summary>
    /// Gets the first constructor argument as a boolean value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a boolean.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsBool(AttributeData, int, bool)"/>
    public static bool GetConstructorArgumentAsBool(this AttributeData attribute, bool defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out bool value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a boolean value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a boolean.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The boolean value of the constructor argument, or the default value.</returns>
    public static bool GetConstructorArgumentAsBool(this AttributeData attribute, int index, bool defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out bool value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a byte value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a byte.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsByte(AttributeData, int, byte)"/>
    public static byte GetConstructorArgumentAsByte(this AttributeData attribute, byte defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out byte value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a byte value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a byte.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The byte value of the constructor argument, or the default value.</returns>
    public static byte GetConstructorArgumentAsByte(this AttributeData attribute, int index, byte defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out byte value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a char value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a char.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsChar(AttributeData, int, char)"/>
    public static char GetConstructorArgumentAsChar(this AttributeData attribute, char defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out char value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a char value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a char.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The char value of the constructor argument, or the default value.</returns>
    public static char GetConstructorArgumentAsChar(this AttributeData attribute, int index, char defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out char value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a decimal value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a decimal.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsDecimal(AttributeData, int, decimal)"/>
    public static decimal GetConstructorArgumentAsDecimal(this AttributeData attribute, decimal defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out decimal value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a decimal value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a decimal.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The decimal value of the constructor argument, or the default value.</returns>
    public static decimal GetConstructorArgumentAsDecimal(this AttributeData attribute, int index, decimal defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out decimal value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a double value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a double.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsDouble(AttributeData, int, double)"/>
    public static double GetConstructorArgumentAsDouble(this AttributeData attribute, double defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out double value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a double value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a double.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The double value of the constructor argument, or the default value.</returns>
    public static double GetConstructorArgumentAsDouble(this AttributeData attribute, int index, double defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out double value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a enum value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a enum.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsEnum{TEnum}(AttributeData, int, TEnum, bool)"/>
    public static TEnum GetConstructorArgumentAsEnum<TEnum>(this AttributeData attribute, TEnum defaultValue = default, bool ignoreEnumType = false)
        where TEnum : struct, Enum
        => TryGetConstructorArgument(attribute, 0, out TEnum value, ignoreEnumType) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a enum value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a enum.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to convert the argument to.</typeparam>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <param name="ignoreEnumType">Whether to ignore the enum type matches when converting the argument.</param>
    /// <returns>The enum value of the constructor argument, or the default value.</returns>
    public static TEnum GetConstructorArgumentAsEnum<TEnum>(this AttributeData attribute, int index, TEnum defaultValue = default, bool ignoreEnumType = false)
        where TEnum : struct, Enum
        => TryGetConstructorArgument(attribute, index, out TEnum value, ignoreEnumType) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a float value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a float.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsFloat(AttributeData, int, float)"/>
    public static float GetConstructorArgumentAsFloat(this AttributeData attribute, float defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out float value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a float value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a float.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The float value of the constructor argument, or the default value.</returns>
    public static float GetConstructorArgumentAsFloat(this AttributeData attribute, int index, float defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out float value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a int value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a int.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsInt(AttributeData, int, int)"/>
    public static int GetConstructorArgumentAsInt(this AttributeData attribute, int defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out int value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a int value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a int.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The int value of the constructor argument, or the default value.</returns>
    public static int GetConstructorArgumentAsInt(this AttributeData attribute, int index, int defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out int value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a long value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a long.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsLong(AttributeData, int, long)"/>
    public static long GetConstructorArgumentAsLong(this AttributeData attribute, long defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out long value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a long value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a long.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The long value of the constructor argument, or the default value.</returns>
    public static long GetConstructorArgumentAsLong(this AttributeData attribute, int index, long defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out long value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a sbyte value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a sbyte.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsSByte(AttributeData, int, sbyte)"/>
    public static sbyte GetConstructorArgumentAsSByte(this AttributeData attribute, sbyte defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out sbyte value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a sbyte value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a sbyte.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The sbyte value of the constructor argument, or the default value.</returns>
    public static sbyte GetConstructorArgumentAsSByte(this AttributeData attribute, int index, sbyte defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out sbyte value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a short value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a short.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsShort(AttributeData, int, short)"/>
    public static short GetConstructorArgumentAsShort(this AttributeData attribute, short defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out short value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a short value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a short.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The short value of the constructor argument, or the default value.</returns>
    public static short GetConstructorArgumentAsShort(this AttributeData attribute, int index, short defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out short value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a string value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a string.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsString(AttributeData, int, string)"/>
    public static string? GetConstructorArgumentAsString(this AttributeData attribute, string? defaultValue = null)
        => TryGetConstructorArgument(attribute, 0, out string? value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a string value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a string.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The string value of the constructor argument, or the default value.</returns>
    public static string? GetConstructorArgumentAsString(this AttributeData attribute, int index, string? defaultValue = null)
        => TryGetConstructorArgument(attribute, index, out string? value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a uint value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a uint.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsUInt(AttributeData, int, uint)"/>
    public static uint GetConstructorArgumentAsUInt(this AttributeData attribute, uint defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out uint value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a uint value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a uint.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The uint value of the constructor argument, or the default value.</returns>
    public static uint GetConstructorArgumentAsUInt(this AttributeData attribute, int index, uint defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out uint value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a ulong value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a ulong.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsULong(AttributeData, int, ulong)"/>
    public static ulong GetConstructorArgumentAsULong(this AttributeData attribute, ulong defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out ulong value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a ulong value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a ulong.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The ulong value of the constructor argument, or the default value.</returns>
    public static ulong GetConstructorArgumentAsULong(this AttributeData attribute, int index, ulong defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out ulong value) ? value : defaultValue;

    /// <summary>
    /// Gets the first constructor argument as a ushort value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a ushort.
    /// </summary>
    /// <inheritdoc cref="GetConstructorArgumentAsUShort(AttributeData, int, ushort)"/>
    public static ushort GetConstructorArgumentAsUShort(this AttributeData attribute, ushort defaultValue = default)
        => TryGetConstructorArgument(attribute, 0, out ushort value) ? value : defaultValue;

    /// <summary>
    /// Gets the constructor argument at the specified index as a ushort value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a ushort.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">Index of the constructor argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The ushort value of the constructor argument, or the default value.</returns>
    public static ushort GetConstructorArgumentAsUShort(this AttributeData attribute, int index, ushort defaultValue = default)
        => TryGetConstructorArgument(attribute, index, out ushort value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a boolean value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a boolean.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The boolean value of the named argument, or the default value.</returns>
    public static bool GetNamedArgumentAsBool(this AttributeData attribute, string argumentName, bool defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out bool value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a byte value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a byte.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The byte value of the named argument, or the default value.</returns>
    public static byte GetNamedArgumentAsByte(this AttributeData attribute, string argumentName, byte defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out byte value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a char value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a char.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The char value of the named argument, or the default value.</returns>
    public static char GetNamedArgumentAsChar(this AttributeData attribute, string argumentName, char defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out char value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a decimal value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a decimal.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The decimal value of the named argument, or the default value.</returns>
    public static decimal GetNamedArgumentAsDecimal(this AttributeData attribute, string argumentName, decimal defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out decimal value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a double value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a double.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The double value of the named argument, or the default value.</returns>
    public static double GetNamedArgumentAsDouble(this AttributeData attribute, string argumentName, double defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out double value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a enum value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a enum.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to convert the argument to.</typeparam>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <param name="ignoreEnumType">Whether to ignore the enum type matches when converting the argument.</param>
    /// <returns>The enum value of the named argument, or the default value.</returns>
    public static TEnum GetNamedArgumentAsEnum<TEnum>(this AttributeData attribute, string argumentName, TEnum defaultValue = default, bool ignoreEnumType = false)
        where TEnum : struct, Enum
        => TryGetNamedArgument(attribute, argumentName, out TEnum value, ignoreEnumType) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a float value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a float.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The float value of the named argument, or the default value.</returns>
    public static float GetNamedArgumentAsFloat(this AttributeData attribute, string argumentName, float defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out float value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a int value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a int.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The int value of the named argument, or the default value.</returns>
    public static int GetNamedArgumentAsInt(this AttributeData attribute, string argumentName, int defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out int value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a long value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a long.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The long value of the named argument, or the default value.</returns>
    public static long GetNamedArgumentAsLong(this AttributeData attribute, string argumentName, long defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out long value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a sbyte value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a sbyte.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The sbyte value of the named argument, or the default value.</returns>
    public static sbyte GetNamedArgumentAsSByte(this AttributeData attribute, string argumentName, sbyte defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out sbyte value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a short value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a short.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The short value of the named argument, or the default value.</returns>
    public static short GetNamedArgumentAsShort(this AttributeData attribute, string argumentName, short defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out short value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a string value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a string.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The string value of the named argument, or the default value.</returns>
    public static string? GetNamedArgumentAsString(this AttributeData attribute, string argumentName, string? defaultValue = null)
        => TryGetNamedArgument(attribute, argumentName, out string? value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a uint value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a uint.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The uint value of the named argument, or the default value.</returns>
    public static uint GetNamedArgumentAsUInt(this AttributeData attribute, string argumentName, uint defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out uint value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a ulong value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a ulong.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The ulong value of the named argument, or the default value.</returns>
    public static ulong GetNamedArgumentAsULong(this AttributeData attribute, string argumentName, ulong defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out ulong value) ? value : defaultValue;

    /// <summary>
    /// Gets the named argument as a ushort value,
    /// or returns the specified default value if the argument is absent or cannot be converted to a ushort.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="defaultValue">Default value to return if the argument is absent or cannot be converted.</param>
    /// <returns>The ushort value of the named argument, or the default value.</returns>
    public static ushort GetNamedArgumentAsUShort(this AttributeData attribute, string argumentName, ushort defaultValue = default)
        => TryGetNamedArgument(attribute, argumentName, out ushort value) ? value : defaultValue;

    /// <summary>
    /// Tries to get the first constructor argument of the specified attribute.
    /// </summary>
    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(AttributeData attribute, out TypedConstant argument)
        => TryGetConstructorArgument(attribute, 0, out argument);

    /// <summary>
    /// Tries to get the constructor argument at the specified index of the specified attribute.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="index">The index of the constructor argument to retrieve.</param>
    /// <param name="argument">The retrieved constructor argument is result is true.</param>
    /// <returns>True if the argument was successfully retrieved; otherwise, false.</returns>
    public static bool TryGetConstructorArgument(AttributeData attribute, int index, out TypedConstant argument)
    {
        if (attribute.ConstructorArguments.Length > index)
        {
            argument = attribute.ConstructorArguments[index];
            return true;
        }

        argument = default;
        return false;
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out bool value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out bool value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsBool(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out byte value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out byte value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsByte(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out char value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out char value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsChar(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out decimal value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out decimal value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsDecimal(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out double value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out double value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsDouble(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument<TEnum>(this AttributeData attribute, out TEnum value, bool ignoreEnumType = false)
        where TEnum : struct, Enum
        => TryGetConstructorArgument(attribute, 0, out value, ignoreEnumType);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument<TEnum>(this AttributeData attribute, int index, out TEnum value, bool ignoreEnumType = false)
        where TEnum : struct, Enum
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsEnum(out value, ignoreEnumType);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out float value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out float value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsFloat(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out int value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out int value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsInt(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out long value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out long value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsLong(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out sbyte value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out sbyte value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsSByte(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out short value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out short value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsShort(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out string? value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, [NotNullWhen(true)] out string? value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsString(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out uint value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out uint value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsUInt(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out ulong value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out ulong value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsULong(out value);
    }

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, out ushort value)
        => TryGetConstructorArgument(attribute, 0, out value);

    /// <inheritdoc cref="TryGetConstructorArgument(AttributeData, int, out TypedConstant)"/>
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out ushort value)
    {
        if (!TryGetConstructorArgument(attribute, index, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsUShort(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="argument">The retrieved named argument.</param>
    /// <returns>True if the named argument was found and is not an error; otherwise, false.</returns>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out TypedConstant argument)
    {
        foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
        {
            if (StringComparer.Ordinal.Equals(namedArgument.Key, argumentName))
            {
                argument = namedArgument.Value;
                return namedArgument.Value.Kind is not TypedConstantKind.Error;
            }
        }

        argument = default;
        return false;
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute with the specified kind.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="kind">The kind of the named argument to retrieve.</param>
    /// <param name="argument">The retrieved named argument.</param>
    /// <returns>True if the named argument was found and is of the specified kind; otherwise, false.</returns>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, TypedConstantKind kind, out TypedConstant argument)
    {
        foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
        {
            if (StringComparer.Ordinal.Equals(namedArgument.Key, argumentName))
            {
                argument = namedArgument.Value;
                return kind == namedArgument.Value.Kind;
            }
        }

        argument = default;
        return false;
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a boolean.
    /// </summary>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <param name="argumentName">Name of the named argument to retrieve.</param>
    /// <param name="value">The retrieved named argument.</param>
    /// <returns>True if the named argument was found and has valid value; otherwise, false.</returns>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out bool value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsBool(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a byte.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out byte value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsByte(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a char.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out char value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsChar(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a decimal.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out decimal value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsDecimal(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a double.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out double value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsDouble(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as an enum.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument<TEnum>(this AttributeData attribute, string argumentName, out TEnum value, bool ignoreEnumType = false)
        where TEnum : struct, Enum
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Enum, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsEnum(out value, ignoreEnumType);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a float.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out float value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsFloat(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as an int.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out int value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsInt(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a long.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out long value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsLong(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a sbyte.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out sbyte value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsSByte(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a short.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out short value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsShort(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a string.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, [NotNullWhen(true)] out string? value)
    {
        if (attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument) && argument.IsString(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a uint.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out uint value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsUInt(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a ulong.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out ulong value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsULong(out value);
    }

    /// <summary>
    /// Tries to get the named argument of the specified attribute as a ushort.
    /// </summary>
    /// <inheritdoc cref="TryGetNamedArgument(AttributeData, string, out bool)"/>
    public static bool TryGetNamedArgument(this AttributeData attribute, string argumentName, out ushort value)
    {
        if (!attribute.TryGetNamedArgument(argumentName, TypedConstantKind.Primitive, out TypedConstant argument))
        {
            value = default;
            return false;
        }

        return argument.IsUShort(out value);
    }
}
