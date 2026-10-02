using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Extracts the primitive, symbol-free data the pipeline needs from Roslyn symbols. All symbol
/// access happens here inside the attribute transform so no <c>ISymbol</c> ever enters a model.
/// </summary>
internal static class SymbolExtraction
{
    /// <summary>
    /// Determines the supported return type (string, byte[], or unsupported).
    /// </summary>
    /// <param name="symbol">The symbol to determine the return type for.</param>
    /// <returns>The supported return type.</returns>
    public static ReturnType GetReturnType(ISymbol symbol)
    {
        ITypeSymbol? returnType = symbol switch
        {
            IMethodSymbol methodSymbol => methodSymbol.ReturnType,
            IPropertySymbol propertySymbol => propertySymbol.Type,
            _ => null,
        };

        return returnType is null ? ReturnType.Unsupported
            : returnType.SpecialType == SpecialType.System_String ? ReturnType.String
            : returnType is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte } ? ReturnType.ByteArray
            : ReturnType.Unsupported;
    }

    /// <summary>
    /// Classifies the member as a property or a method.
    /// </summary>
    /// <param name="symbol">The symbol to classify.</param>
    /// <returns>The kind of member.</returns>
    public static MemberKind GetMemberKind(ISymbol symbol)
        => symbol is IMethodSymbol ? MemberKind.Method
         : symbol is IPropertySymbol ? MemberKind.Property
         : symbol is INamedTypeSymbol { TypeKind: TypeKind.Class } ? MemberKind.Class
         : MemberKind.Unknown;
}
