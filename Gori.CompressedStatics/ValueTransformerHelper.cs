using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics;

/// <summary>
/// Helper for converting transformer string to instance.
/// </summary>
internal static class ValueTransformerHelper
{
    /// <summary>
    /// Provides details error.
    /// </summary>
    public enum Error
    {
        /// <summary>
        /// No error, result is true.
        /// </summary>
        None,

        /// <summary>
        /// Input string format is incorrect.
        /// </summary>
        InvalidFormat,

        /// <summary>
        /// Was unable to locate type.
        /// </summary>
        TypeNotFound,

        /// <summary>
        /// Type located is not a concrete class.
        /// </summary>
        TypeNotConcreteClass,

        /// <summary>
        /// Multiple types or members found in loaded assemblies matching provided input.
        /// </summary>
        IsAmbiguous,

        /// <summary>
        /// Type found is a generic class.
        /// </summary>
        TypeIsGeneric,

        /// <summary>
        /// Type found is not of the required type.
        /// </summary>
        TypeNotAssignable,

        /// <summary>
        /// Type does not contain public parameterless constructor.
        /// </summary>
        TypeHasNoPublicParameterlessConstructor,

        /// <summary>
        /// Member not found.
        /// </summary>
        MemberNotFound,

        /// <summary>
        /// Member found not of the required type.
        /// </summary>
        MemberNotAssignable,

        /// <summary>
        /// A reflection exception was thrown.
        /// </summary>
        ExceptionThrown,

        /// <summary>
        /// Unknown error.
        /// </summary>
        UnknownError,

        /// <summary>No public constructor accepts the explicit argument types.</summary>
        ConstructorNotFound,

        /// <summary>There is no unique most-specific matching public constructor.</summary>
        ConstructorIsAmbiguous,

        /// <summary>An argument cannot be materialized from its attribute constant.</summary>
        InvalidArgument,
    }

    /// <summary>
    /// Takes transformer input and extracts type and member values if valid format.
    /// Null is classed as valid format to use no transformers, so type can be null even when true.
    /// Does not validate the existence of type or member just that the format was correct.
    /// </summary>
    /// <param name="value">The Transformer input value.</param>
    /// <param name="type">Type extracted from value.</param>
    /// <param name="member">Member extracted from value if provided.</param>
    /// <returns>True if value was valid user input.</returns>
    public static bool IsValidTransformerValue(string? value, out string? type, out string? member)
    {
        type = null;
        member = null;

        if (value is null)
        {
            return true;
        }

        if (value.IsWhiteSpace())
        {
            return false;
        }

        string[] split = value.Split(["::"], StringSplitOptions.None);
        type = split[0].Trim();

        switch (split.Length)
        {
            case 1:
                return type.Length > 0;

            case 2:
                member = split[1].Trim();
                return type.Length > 0 && member.Length > 0;

            case > 2:
                member = split[1].Trim();
                return false;

            // Should never hit default as Split should always return at least 1 element, but just in case.
            default:
                type = null;
                return false;
        }
    }

    /// <summary>
    /// Transforms valid input, returning diagnostics separately from the processed payload.
    /// Binary members use the Base64 transformer by default and are decoded exactly once.
    /// </summary>
    /// <param name="attribute">The resolved input member.</param>
    /// <param name="compilation">The input compilation.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The processed member and optional diagnostic. Failure permanently invalidates the member.</returns>
    public static ProcessingResult<CompressAttributeInfo> TransformInput(
        this CompressAttributeInfo attribute,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!attribute.IsValid || attribute.SourceValue is null)
        {
            return new(attribute);
        }

        try
        {
            switch (attribute.ReturnType)
            {
                case ReturnType.String:
                    if (attribute.Transformer is null)
                    {
                        return new(attribute);
                    }

                    if (!TryGetTransformer(attribute.Transformer, attribute.TransformerMember, attribute.TransformerArguments, out IStringTransformer? stringTransformer, out Error stringError))
                    {
                        return new(attribute.Invalidate(), ToDiagnostic(attribute, stringError));
                    }

                    bool stringSuccess = stringTransformer.TryTransform(attribute, compilation, out string? text, out DiagnosticInfo? stringDiagnostic, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (stringDiagnostic?.Severity == DiagnosticSeverity.Error || (!stringSuccess && stringDiagnostic is not null))
                    {
                        return new(attribute.Invalidate(), stringDiagnostic);
                    }

                    if (!stringSuccess || text is null)
                    {
                        return MissingOutput(attribute);
                    }

                    return new(attribute.WithSourceValue(text), stringDiagnostic);

                case ReturnType.ByteArray:
                    IBinaryTransformer? binaryTransformer = null;
                    if (attribute.Transformer is not null
                        && !TryGetTransformer(attribute.Transformer, attribute.TransformerMember, attribute.TransformerArguments, out binaryTransformer, out Error binaryError))
                    {
                        return new(attribute.Invalidate(), ToDiagnostic(attribute, binaryError));
                    }

                    binaryTransformer ??= Base64Transformer.Instance;
                    bool binarySuccess = binaryTransformer.TryTransform(attribute, compilation, out byte[]? bytes, out DiagnosticInfo? binaryDiagnostic, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (binaryDiagnostic?.Severity == DiagnosticSeverity.Error || (!binarySuccess && binaryDiagnostic is not null))
                    {
                        return new(attribute.Invalidate(), binaryDiagnostic);
                    }

                    if (!binarySuccess || bytes is null)
                    {
                        return MissingOutput(attribute);
                    }

                    return new(attribute.WithSourceBytes(bytes), binaryDiagnostic);

                default:
                    throw new InvalidOperationException("Unsupported input transformation return type.");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new(attribute.Invalidate(), new DiagnosticInfo(
                DiagnosticDescriptors.TransformerExceptionThrown,
                attribute.MemberName,
                attribute.Transformer ?? nameof(Base64Transformer),
                exception.Message));
        }
    }

    /// <summary>
    /// Attempts to locate transformer based on input string.
    /// </summary>
    /// <param name="transformerName">The transformer type name with optional assembly qualification.</param>
    /// <param name="transformerMember">Optional member name to locate on the transformer type.</param>
    /// <param name="transformer">Transformer instance if found.</param>
    /// <param name="error">The error if the transformer was not found or is ambiguous.</param>
    /// <returns>True if located; otherwise false.</returns>
    public static bool TryGetTransformer(string transformerName, string? transformerMember, [NotNullWhen(true)] out IStringTransformer? transformer, out Error error)
        => TryGetTransformer<IStringTransformer>(transformerName, transformerMember, default, out transformer, out error);

    /// <summary>
    /// Attempts to locate transformer based on input string.
    /// </summary>
    /// <param name="transformerName">The transformer type name with optional assembly qualification.</param>
    /// <param name="transformerMember">Optional member name to locate on the transformer type.</param>
    /// <param name="transformer">Transformer instance if found.</param>
    /// <param name="error">The error if the transformer was not found or is ambiguous.</param>
    /// <returns>True if located; otherwise false.</returns>
    public static bool TryGetTransformer(string transformerName, string? transformerMember, [NotNullWhen(true)] out IBinaryTransformer? transformer, out Error error)
        => TryGetTransformer<IBinaryTransformer>(transformerName, transformerMember, default, out transformer, out error);

    /// <summary>Loads a string transformer using explicit constructor arguments when supplied.</summary>
    /// <param name="transformerName">The transformer type name with optional assembly qualification.</param>
    /// <param name="arguments">Explicit arguments, or default to use the implicit Instance fallback.</param>
    /// <param name="transformer">The loaded transformer.</param>
    /// <param name="error">The loading error.</param>
    /// <returns>Whether the transformer was loaded.</returns>
    public static bool TryGetTransformer(string transformerName, ImmutableArray<TransformerArgument> arguments, [NotNullWhen(true)] out IStringTransformer? transformer, out Error error)
        => TryGetTransformer<IStringTransformer>(transformerName, null, arguments, out transformer, out error);

    /// <summary>
    /// Loads a binary transformer using explicit constructor arguments when supplied.
    /// </summary>
    /// <param name="transformerName">The transformer type name with optional assembly qualification.</param>
    /// <param name="arguments">Explicit arguments, or default to use the implicit Instance fallback.</param>
    /// <param name="transformer">The loaded transformer.</param>
    /// <param name="error">The loading error.</param>
    /// <returns>Whether the transformer was loaded.</returns>
    public static bool TryGetTransformer(string transformerName, ImmutableArray<TransformerArgument> arguments, [NotNullWhen(true)] out IBinaryTransformer? transformer, out Error error)
        => TryGetTransformer<IBinaryTransformer>(transformerName, null, arguments, out transformer, out error);

    private static ProcessingResult<CompressAttributeInfo> MissingOutput(CompressAttributeInfo attribute)
        => new(attribute.Invalidate(), new DiagnosticInfo(
            DiagnosticDescriptors.TransformerExceptionThrown,
            [attribute.MemberName, attribute.Transformer ?? nameof(Base64Transformer), "The transformer returned no output or failure diagnostic."]));

    private static DiagnosticInfo ToDiagnostic(CompressAttributeInfo attribute, ValueTransformerHelper.Error error)
        => new(
            error switch
            {
                Error.ConstructorNotFound => DiagnosticDescriptors.TransformerConstructorNotFound,
                Error.ConstructorIsAmbiguous => DiagnosticDescriptors.TransformerConstructorIsAmbiguous,
                Error.InvalidArgument => DiagnosticDescriptors.InvalidTransformerArgument,
                Error.InvalidFormat => DiagnosticDescriptors.InvalidTransformerFormat,
                Error.TypeNotFound => DiagnosticDescriptors.TransformerTypeNotFound,
                Error.TypeNotConcreteClass => DiagnosticDescriptors.TransformerTypeNotConcreteClass,
                Error.IsAmbiguous => DiagnosticDescriptors.TransformerIsAmbiguous,
                Error.TypeIsGeneric => DiagnosticDescriptors.TransformerTypeIsGeneric,
                Error.TypeNotAssignable => DiagnosticDescriptors.TransformerTypeNotAssignable,
                Error.TypeHasNoPublicParameterlessConstructor => DiagnosticDescriptors.TransformerTypeHasNoPublicParameterlessConstructor,
                Error.MemberNotFound => DiagnosticDescriptors.TransformerMemberNotFound,
                Error.MemberNotAssignable => DiagnosticDescriptors.TransformerMemberNotAssignable,
                Error.ExceptionThrown => DiagnosticDescriptors.TransformerExceptionThrownLoadingInstance,
                _ => DiagnosticDescriptors.TransformerUnknownErrorLoadingInstance,
            },
            attribute.MemberName);

    /// <summary>
    /// Tries to locate type by name from loaded assemblies.
    /// Valid formats in order of attempt.
    /// <list type="table">
    /// <item>
    /// <term>Full.Type.Name, AssemblyName</term>
    /// <description>Type.GetType(string) will be used to locate type.</description>
    /// </item>
    /// <item>
    /// <term>Full.Type.Name</term>
    /// <description>All loaded assemblies will be searched for a type with the given full name. Must only find single match to be valid.</description>
    /// </item>
    /// <item>
    /// <term>ShortTypeName</term>
    /// <description>All loaded assemblies will be searched for a type with the given name. Must only find single match to be valid.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="typeName">Type name to locate.</param>
    /// <param name="type">The located type if found.</param>
    /// <param name="error">The error if the type was not found or is ambiguous.</param>
    /// <returns>True if the type was found; otherwise, false.</returns>
    private static bool TryGetType(string typeName, [NotNullWhen(true)] out Type? type, out Error error)
    {
        type = Type.GetType(typeName);
        if (type is not null)
        {
            error = Error.None;
            return true;
        }

        foreach (Assembly? assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = assembly.GetType(typeName);
            if (t is not null)
            {
                if (type is null)
                {
                    type = t;
                }
                else
                {
                    error = Error.IsAmbiguous;
                    return false;
                }
            }
        }

        if (type is not null)
        {
            error = Error.None;
            return true;
        }

        if (typeName.Contains('.'))
        {
            error = Error.TypeNotFound;
            return false;
        }

        foreach (Assembly? assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type? t in assembly.GetTypes())
            {
                if (t.Name == typeName)
                {
                    if (type is null)
                    {
                        type = t;
                    }
                    else
                    {
                        error = Error.IsAmbiguous;
                        return false;
                    }
                }
            }
        }

        if (type is null)
        {
            error = Error.TypeNotFound;
            return false;
        }

        error = Error.None;
        return true;
    }

    private static bool TryGetTransformerType(string typeName, [NotNullWhen(true)] out Type? type, out Error error)
    {
        const string suffix = "Transformer";
        if (typeName.IndexOfAny(['.', ',', '+', '[', ']']) < 0)
        {
            Assembly builtInAssembly = typeof(Base64Transformer).Assembly;
            string qualifiedName = Const.Namespace + "." + typeName;
            type = builtInAssembly.GetType(qualifiedName)
                ?? (typeName.EndsWith(suffix, StringComparison.Ordinal) ? null : builtInAssembly.GetType(qualifiedName + suffix));
            if (type is not null)
            {
                error = Error.None;
                return true;
            }
        }

        if (TryGetType(typeName, out type, out error) || error != Error.TypeNotFound)
        {
            return type is not null && error == Error.None;
        }

        int depth = 0;
        int assemblySeparator = typeName.Length;
        for (int index = 0; index < typeName.Length; index++)
        {
            if (typeName[index] == '[')
            {
                depth++;
            }
            else if (typeName[index] == ']')
            {
                depth--;
            }
            else if (typeName[index] == ',' && depth == 0)
            {
                assemblySeparator = index;
                break;
            }
        }

        string name = typeName[..assemblySeparator].TrimEnd();
        return !name.EndsWith(suffix, StringComparison.Ordinal) && TryGetType(name + suffix + typeName[assemblySeparator..], out type, out error);
    }

    private static bool IsReflectionFailure(Exception exception)
        => exception is ArgumentException
            or TypeLoadException
            or ReflectionTypeLoadException
            or FileNotFoundException
            or FileLoadException
            or BadImageFormatException
            or MemberAccessException
            or AmbiguousMatchException
            or TargetInvocationException
            or TargetException
            or TargetParameterCountException
            or TypeInitializationException
            or NotSupportedException
            or System.Security.SecurityException;

    private static bool TryGetTypeMember<T>(Type type, string memberName, bool mustBeInputType, [NotNullWhen(true)] out T? transformer, out Error error)
        where T : class
    {
        FieldInfo? field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
        if (field is not null)
        {
            if (mustBeInputType && field.FieldType != type)
            {
                transformer = null;
                error = Error.MemberNotAssignable;
                return false;
            }

            if (!typeof(T).IsAssignableFrom(field.FieldType))
            {
                transformer = null;
                error = Error.MemberNotAssignable;
                return false;
            }

            if (field.GetValue(null) is not T fieldInstance)
            {
                transformer = null;
                error = Error.UnknownError;
                return false;
            }

            transformer = fieldInstance;
            error = Error.None;
            return true;
        }

        PropertyInfo? property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
        if (property is null
            || property.GetMethod is not { IsPublic: true, IsStatic: true })
        {
            transformer = null;
            error = Error.MemberNotFound;
            return false;
        }

        if (property.GetIndexParameters().Length != 0)
        {
            transformer = null;
            error = Error.MemberNotAssignable;
            return false;
        }

        if (mustBeInputType && property.PropertyType != type)
        {
            transformer = null;
            error = Error.MemberNotAssignable;
            return false;
        }

        if (!typeof(T).IsAssignableFrom(property.PropertyType))
        {
            transformer = null;
            error = Error.MemberNotAssignable;
            return false;
        }

        if (property.GetValue(null) is not T propertyInstance)
        {
            transformer = null;
            error = Error.UnknownError;
            return false;
        }

        transformer = propertyInstance;
        error = Error.None;
        return true;
    }

    private static bool TryResolveArgument(TransformerArgument argument, out object? value)
    {
        try
        {
            return TryResolveArgumentCore(argument, out value);
        }
        catch (Exception exception) when (IsReflectionFailure(exception))
        {
            value = null;
            return false;
        }
    }

    private static bool TryResolveArgumentCore(TransformerArgument argument, out object? value)
    {
        value = argument.Value;
        switch (argument.Kind)
        {
            case TypedConstantKind.Primitive:
                return true;
            case TypedConstantKind.Type:
                if (value is null)
                {
                    return true;
                }

                if (value is string typeName && TryGetType(typeName, out Type? type, out _))
                {
                    value = type;
                    return true;
                }

                return false;
            case TypedConstantKind.Enum:
                if (argument.TypeName is not null && value is not null
                    && TryGetType(argument.TypeName, out Type? enumType, out _) && enumType.IsEnum)
                {
                    value = Enum.ToObject(enumType, value);
                    return true;
                }

                return false;
            case TypedConstantKind.Array:
                value = null;
                if (argument.Elements.IsDefault)
                {
                    return true;
                }

                if (argument.TypeName is null || !TryGetType(argument.TypeName, out Type? arrayType, out _)
                    || !arrayType.IsArray || arrayType.GetArrayRank() != 1)
                {
                    return false;
                }

                Type elementType = arrayType.GetElementType()!;
                var array = Array.CreateInstance(elementType, argument.Elements.Length);
                for (int i = 0; i < argument.Elements.Length; i++)
                {
                    if (!TryResolveArgument(argument.Elements[i], out object? element)
                        || !CanAssign(elementType, element))
                    {
                        return false;
                    }

                    array.SetValue(element, i);
                }

                value = array;
                return true;
            default:
                return false;
        }
    }

    private static bool CanAssign(Type parameterType, object? value)
        => value is null
            ? !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) is not null
            : parameterType.IsInstanceOfType(value);

    private static bool TryConstruct<T>(Type type, ImmutableArray<TransformerArgument> arguments, [NotNullWhen(true)] out T? transformer, out Error error)
        where T : class
    {
        transformer = null;
        object?[] values = new object?[arguments.Length];
        for (int i = 0; i < arguments.Length; i++)
        {
            if (!TryResolveArgument(arguments[i], out values[i]))
            {
                error = Error.InvalidArgument;
                return false;
            }
        }

        ConstructorInfo[] candidates = [.. type.GetConstructors()
            .Where(constructor => constructor.GetParameters().Length == values.Length
                && constructor.GetParameters().Select((parameter, index) => CanAssign(parameter.ParameterType, values[index])).All(matches => matches))];
        if (candidates.Length == 0)
        {
            error = Error.ConstructorNotFound;
            return false;
        }

        // A constructor wins only if it is strictly better than every alternative. This avoids
        // reflection enumeration order, numeric coercion, or arbitrary tie-breaking affecting output.
        ConstructorInfo[] best = [.. candidates.Where(candidate => !candidates.Any(other => other != candidate && IsBetterMatch(other, candidate, values)))];
        if (best.Length != 1)
        {
            error = Error.ConstructorIsAmbiguous;
            return false;
        }

        transformer = best[0].Invoke(values) as T;
        error = transformer is null ? Error.UnknownError : Error.None;
        return transformer is not null;
    }

    private static bool IsBetterMatch(ConstructorInfo candidate, ConstructorInfo other, object?[] values)
    {
        ParameterInfo[] left = candidate.GetParameters();
        ParameterInfo[] right = other.GetParameters();
        bool better = false;
        for (int i = 0; i < left.Length; i++)
        {
            Type leftType = left[i].ParameterType;
            Type rightType = right[i].ParameterType;
            if (leftType == rightType)
            {
                continue;
            }

            Type? actualType = values[i]?.GetType();
            if (actualType == leftType)
            {
                better = true;
            }
            else if (actualType == rightType || !rightType.IsAssignableFrom(leftType))
            {
                return false;
            }
            else
            {
                better = true;
            }
        }

        return better;
    }

    private static bool TryGetTransformer<T>(string transformerType, string? transformerMember, ImmutableArray<TransformerArgument> arguments, [NotNullWhen(true)] out T? transformer, out Error error)
        where T : class
    {
        try
        {
            if (!TryGetTransformerType(transformerType, out Type? type, out error))
            {
                transformer = null;
                return false;
            }

            if (type.ContainsGenericParameters)
            {
                transformer = null;
                error = Error.TypeIsGeneric;
                return false;
            }

            if (transformerMember is null)
            {
                if (!type.IsClass || type.IsAbstract)
                {
                    transformer = null;
                    error = Error.TypeNotConcreteClass;
                    return false;
                }

                if (!typeof(T).IsAssignableFrom(type))
                {
                    transformer = null;
                    error = Error.TypeNotAssignable;
                    return false;
                }

                if (!arguments.IsDefault)
                {
                    return TryConstruct(type, arguments, out transformer, out error);
                }

                // Try to get a static member named "Instance" first, if it exists.
                if (TryGetTypeMember(type, "Instance", true, out transformer, out error))
                {
                    return true;
                }

                error = Error.None;

                if (type.GetConstructor(Type.EmptyTypes) is null)
                {
                    transformer = null;
                    error = Error.TypeHasNoPublicParameterlessConstructor;
                    return false;
                }

                if (Activator.CreateInstance(type) is T instance1)
                {
                    transformer = instance1;
                    return true;
                }

                transformer = null;
                error = Error.UnknownError;
                return false;
            }

            return TryGetTypeMember(type, transformerMember, false, out transformer, out error);
        }
        catch (AmbiguousMatchException)
        {
            transformer = null;
            error = Error.IsAmbiguous;
            return false;
        }
        catch (Exception exception) when (IsReflectionFailure(exception))
        {
            transformer = null;
            error = Error.ExceptionThrown;
            return false;
        }
    }
}
