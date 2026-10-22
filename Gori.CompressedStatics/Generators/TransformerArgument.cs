using System.Collections.Immutable;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// An immutable, symbol-free snapshot of an attribute constructor argument, including nested arrays.
/// </summary>
internal readonly struct TransformerArgument : IEquatable<TransformerArgument>
{
    private TransformerArgument(TypedConstantKind kind, string? typeName, object? value, ImmutableArray<TransformerArgument> elements)
    {
        Kind = kind;
        TypeName = typeName;
        Value = value;
        Elements = elements;
    }

    /// <summary>Gets the constant kind.</summary>
    public TypedConstantKind Kind { get; }

    /// <summary>Gets the assembly-qualified runtime type name for enum and array constants.</summary>
    public string? TypeName { get; }

    /// <summary>Gets the primitive value or assembly-qualified name for a typeof constant.</summary>
    public object? Value { get; }

    /// <summary>Gets the nested array elements. Default represents a null array.</summary>
    public ImmutableArray<TransformerArgument> Elements { get; }

    /// <summary>Snapshots a Roslyn attribute constant without retaining symbols.</summary>
    /// <param name="constant">The constant to snapshot.</param>
    /// <returns>The immutable argument.</returns>
    public static TransformerArgument FromConstant(TypedConstant constant)
    {
        object? value = constant.Kind switch
        {
            TypedConstantKind.Type => constant.Value is ITypeSymbol type ? GetRuntimeTypeName(type) : null,
            TypedConstantKind.Array => null,
            _ => constant.Value,
        };

        ImmutableArray<TransformerArgument> elements = constant.Kind == TypedConstantKind.Array && !constant.IsNull
            ? [.. constant.Values.Select(FromConstant)]
            : default;

        return new TransformerArgument(constant.Kind, constant.Type is null ? null : GetRuntimeTypeName(constant.Type), value, elements);
    }

    /// <inheritdoc/>
    public bool Equals(TransformerArgument other)
        => Kind == other.Kind
        && StringComparer.Ordinal.Equals(TypeName, other.TypeName)
        && Equals(Value, other.Value)
        && Elements.IsDefault == other.Elements.IsDefault
        && SequenceEquality.Equals(Elements, other.Elements);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TransformerArgument other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Kind;
            hash = (hash * 397) ^ (TypeName is null ? 0 : StringComparer.Ordinal.GetHashCode(TypeName));
            hash = (hash * 397) ^ (Value?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ Elements.IsDefault.GetHashCode();
            return (hash * 397) ^ SequenceEquality.GetHashCode(Elements);
        }
    }

    private static string GetRuntimeTypeName(ITypeSymbol type)
    {
        string name = GetMetadataTypeName(type);
        ITypeSymbol assemblyType = type;
        while (assemblyType is IArrayTypeSymbol or IPointerTypeSymbol)
        {
            assemblyType = assemblyType is IArrayTypeSymbol array ? array.ElementType : ((IPointerTypeSymbol)assemblyType).PointedAtType;
        }

        // The generated marker enum belongs to the consumer assembly but represents the host's options enum.
        string assemblyIdentity = assemblyType.TypeKind == TypeKind.Enum
            && GetMetadataTypeName(assemblyType) == typeof(CSharpRewriterOptions).FullName
                ? typeof(CSharpRewriterOptions).Assembly.FullName
                : assemblyType.ContainingAssembly.Identity.ToString();

        return name + ", " + assemblyIdentity;
    }

    private static string GetMetadataTypeName(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            return GetMetadataTypeName(array.ElementType) + "[" + new string(',', array.Rank - 1) + "]";
        }

        if (type is IPointerTypeSymbol pointer)
        {
            return GetMetadataTypeName(pointer.PointedAtType) + "*";
        }

        if (type is not INamedTypeSymbol named)
        {
            return type.MetadataName;
        }

        var nesting = new Stack<INamedTypeSymbol>();
        for (INamedTypeSymbol? current = named; current is not null; current = current.ContainingType)
        {
            nesting.Push(current);
        }

        string prefix = named.ContainingNamespace.IsGlobalNamespace ? string.Empty : named.ContainingNamespace.ToDisplayString() + ".";
        string name = prefix + string.Join("+", nesting.Select(item => item.MetadataName));
        if (!named.IsUnboundGenericType)
        {
            ITypeSymbol[] arguments = [.. nesting.SelectMany(item => item.TypeArguments)];
            if (arguments.Length != 0)
            {
                name += "[" + string.Join(",", arguments.Select(argument => "[" + GetRuntimeTypeName(argument) + "]")) + "]";
            }
        }

        return name;
    }
}
