using System.Collections.Immutable;

using Gori.Roslyn;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics;

/// <summary>
/// Provides helper methods for working with compression-related attributes in the Gori.CompressedStatics namespace.
/// </summary>
internal static class AttributeHelper
{
    /// <summary>
    /// Gets the short name of the specified <see cref="CompressAttributeType"/>.
    /// </summary>
    /// <param name="type">The compress attribute type.</param>
    /// <returns>The short name of the compress attribute type.</returns>
    public static string GetShortName(this CompressAttributeType type)
        => type switch
        {
            CompressAttributeType.CompressFile => Const.CompressFileAttributeShortName,
            CompressAttributeType.CompressString => Const.CompressStringAttributeShortName,
            CompressAttributeType.TarCompress => Const.TarCompressAttributeShortName,
            CompressAttributeType.Unknown or _ => string.Empty,
        };

    /// <summary>
    /// Gets the <see cref="CompressAttributeType"/> corresponding to the specified attribute name.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>The corresponding <see cref="CompressAttributeType"/>.</returns>
    public static CompressAttributeType GetCompressAttributeType(string? attributeName)
        => IsCompressFileAttribute(attributeName) ? CompressAttributeType.CompressFile
         : IsCompressStringAttribute(attributeName) ? CompressAttributeType.CompressString
         : IsTarCompressAttribute(attributeName) ? CompressAttributeType.TarCompress
         : CompressAttributeType.Unknown;

    /// <summary>
    /// Check if the given attribute name corresponds to a compress file attribute.
    /// Checks against the full name, name, and short name of the attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to check.</param>
    /// <returns><c>true</c> if the attribute name corresponds to a compress file attribute; otherwise, <c>false</c>.</returns>
    public static bool IsCompressFileAttribute(string? attributeName)
        => attributeName is Const.CompressFileAttributeFullName
                         or Const.CompressFileAttributeFullShortName
                         or Const.CompressFileAttributeName
                         or Const.CompressFileAttributeShortName;

    /// <summary>
    /// Check if the given attribute name corresponds to a compress string attribute.
    /// Checks against the full name, name, and short name of the attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to check.</param>
    /// <returns><c>true</c> if the attribute name corresponds to a compress string attribute; otherwise, <c>false</c>.</returns>
    public static bool IsCompressStringAttribute(string? attributeName)
        => attributeName is Const.CompressStringAttributeFullName
                         or Const.CompressStringAttributeFullShortName
                         or Const.CompressStringAttributeName
                         or Const.CompressStringAttributeShortName;

    /// <summary>
    /// Check if the given attribute name corresponds to a tar compress attribute.
    /// Checks against the full name, name, and short name of the attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to check.</param>
    /// <returns><c>true</c> if the attribute name corresponds to a tar compress attribute; otherwise, <c>false</c>.</returns>
    public static bool IsTarCompressAttribute(string? attributeName)
        => attributeName is Const.TarCompressAttributeFullName
                         or Const.TarCompressAttributeFullShortName
                         or Const.TarCompressAttributeName
                         or Const.TarCompressAttributeShortName;

    /// <summary>
    /// Check if the given attribute name corresponds to any compress attribute (file or string).
    /// </summary>
    /// <param name="attributeName">The name of the attribute to check.</param>
    /// <returns><c>true</c> if the attribute name corresponds to any compress attribute; otherwise, <c>false</c>.</returns>
    public static bool IsCompressAttribute(string? attributeName)
        => IsCompressFileAttribute(attributeName)
        || IsCompressStringAttribute(attributeName);

    /// <summary>
    /// Whether the member is a property covered by its type's <c>TarCompress</c> attribute.
    /// </summary>
    /// <param name="property">The property symbol representing the member to check.</param>
    /// <param name="propertyCacheMode">The cache mode of the property, which must be <c>OnInit</c> for the property to be considered covered.</param>
    /// <param name="algorithm">The compression algorithm specified in the <c>TarCompress</c> attribute of the containing type, if applicable.</param>
    /// <param name="cancellationToken">A cancellation token to observe while performing the operation.</param>
    /// <returns><c>true</c> if the member is a property covered by its type's <c>TarCompress</c> attribute; otherwise, <c>false</c>.</returns>
    public static bool IsTarCoveredMember(IPropertySymbol property, CompressionCacheMode propertyCacheMode, out CompressionAlgorithm algorithm, CancellationToken cancellationToken = default)
    {
        algorithm = default;

        return propertyCacheMode is CompressionCacheMode.OnInit
            && TryGetConfiguration(property.ContainingType, out ImmutableHashSet<string> excluded, out algorithm, cancellationToken)
            && !excluded.Contains(property.Name);
    }

    /// <summary>
    /// Determines whether the specified return type is valid for compression.
    /// </summary>
    /// <param name="returnType">The type symbol representing the return type to check.</param>
    /// <returns><c>true</c> if the return type is valid for compression; otherwise, <c>false</c>.</returns>
    public static bool IsValidReturnType(ITypeSymbol returnType)
        => returnType is not null
        && returnType.NullableAnnotation is not NullableAnnotation.Annotated
        && (returnType.SpecialType == SpecialType.System_String
            || (returnType is IArrayTypeSymbol arrayTypeSymbol
                && arrayTypeSymbol.Rank == 1
                && arrayTypeSymbol.ElementType.NullableAnnotation is not NullableAnnotation.Annotated
                && arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Byte));

    private static bool TryGetConfiguration(
        INamedTypeSymbol type,
        out ImmutableHashSet<string> excludedPropertyNames,
        out CompressionAlgorithm algorithm,
        CancellationToken cancellationToken = default)
    {
        foreach (AttributeData attribute in type.GetAttributes())
        {
            if (!IsTarCompressAttribute(attribute.AttributeClass?.ToDisplayString()))
            {
                cancellationToken.ThrowIfCancellationRequested();
                continue;
            }

            excludedPropertyNames = GetExcludedPropertyNames(attribute);
            algorithm = attribute.GetNamedArgumentAsEnum(nameof(TarCompressAttribute.Algorithm), CompressionAlgorithm.Auto);
            return true;
        }

        excludedPropertyNames = [];
        algorithm = CompressionAlgorithm.Auto;
        return false;
    }

    private static ImmutableHashSet<string> GetExcludedPropertyNames(AttributeData attribute)
    {
        if (attribute.TryGetNamedArgument(nameof(TarCompressAttribute.Exclude), TypedConstantKind.Array, out TypedConstant excludeArgument))
        {
            ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder(StringComparer.Ordinal);
            foreach (TypedConstant value in excludeArgument.Values)
            {
                if (value.Value is string propertyName && !string.IsNullOrWhiteSpace(propertyName))
                {
                    _ = builder.Add(propertyName);
                }
            }

            return builder.ToImmutable();
        }

        return [];
    }
}
