#pragma warning disable IDE0130

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gori.Roslyn;

/// <summary>
/// Provides extension methods for <see cref="ISymbol"/> instances.
/// </summary>
internal static class SymbolExtensions
{
    /// <summary>
    /// Builds the symbol-free description of a containing type.
    /// </summary>
    /// <param name="type">The named type symbol to extract information from.</param>
    /// <returns>An <see cref="EquatableTypeInfo"/> instance containing the extracted information.</returns>
    public static EquatableTypeInfo CreateEquatableTypeInfo(this INamedTypeSymbol type)
    {
        string @namespace = type.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? containingNamespace.ToDisplayString()
            : string.Empty;

        ImmutableArray<TypeDeclaration>.Builder declarations = ImmutableArray.CreateBuilder<TypeDeclaration>();
        BuildDeclarations(type, declarations);

        string fullyQualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        bool hasUserStaticConstructor = false;
        foreach (ISymbol member in type.GetMembers())
        {
            switch (member)
            {
                case IMethodSymbol { MethodKind: MethodKind.StaticConstructor }:
                    hasUserStaticConstructor = true;
                    break;

                default:
                    break;
            }
        }

        var location = LocationInfo.From(type);

        return new EquatableTypeInfo(@namespace, declarations.ToImmutable(), fullyQualifiedName, hasUserStaticConstructor, location);
    }

    /// <summary>
    /// Returns whether the symbol is declared <c>partial</c> in any of its declarations.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="cancellationToken">A cancellation token to observe while checking the symbol.</param>
    /// <returns><c>true</c> if the symbol is declared partial; otherwise, <c>false</c>.</returns>
    public static bool IsPartial(this ISymbol symbol, CancellationToken cancellationToken = default)
    {
        foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
        {
            SyntaxTokenList modifiers = reference.GetSyntax(cancellationToken) switch
            {
                ClassDeclarationSyntax classDecl => classDecl.Modifiers,
                PropertyDeclarationSyntax property => property.Modifiers,
                MethodDeclarationSyntax method => method.Modifiers,
                _ => default,
            };

            if (modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return false;
    }

    /// <summary>
    /// Returns whether the given type symbol is a or within a generic type (full hierarchy).
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <returns><c>true</c> if the type symbol is a or within a generic type; otherwise, <c>false</c>.</returns>
    public static bool IsWithinGenericType(this ISymbol typeSymbol)
    {
        INamedTypeSymbol? namedTypeSymbol = typeSymbol as INamedTypeSymbol ?? typeSymbol?.ContainingType;
        return namedTypeSymbol?.IsGenericType() ?? false;
    }

    /// <summary>
    /// Returns whether the given type symbol is a generic type (full hierarchy).
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <returns><c>true</c> if the type symbol is a generic type; otherwise, <c>false</c>.</returns>
    public static bool IsGenericType(this INamedTypeSymbol typeSymbol)
    {
        for (INamedTypeSymbol? current = typeSymbol; current is not null; current = current.ContainingType)
        {
            if (current.IsGenericType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Maps a symbol's declared accessibility to its C# keyword.
    /// </summary>
    /// <param name="accessibility">The accessibility to map.</param>
    /// <returns>The C# keyword representing the accessibility.</returns>
    public static string ToKeywords(this Accessibility accessibility)
        => accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Private => "private",
            Accessibility.NotApplicable or _ => "public",
        };

    /// <summary>
    /// Tries to get the attribute data for a specific attribute applied to a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to check for the attribute.</param>
    /// <param name="attributeFullName">The full name of the attribute to look for.</param>
    /// <param name="attribute">The attribute data if found.</param>
    /// <returns><c>true</c> if the attribute is found; otherwise, <c>false</c>.</returns>
    public static bool TryGetAttribute(this ISymbol symbol, string attributeFullName, [NotNullWhen(true)] out AttributeData? attribute)
    {
        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == attributeFullName)
            {
                attribute = attr;
                return true;
            }
        }

        attribute = null;
        return false;
    }

    private static void BuildDeclarations(INamedTypeSymbol type, ImmutableArray<TypeDeclaration>.Builder declarations)
    {
        if (type.ContainingType is not null)
        {
            BuildDeclarations(type.ContainingType, declarations);
        }

        declarations.Add(new TypeDeclaration(GetTypeKeyword(type), GetTypeName(type)));
    }

    [SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Covered by default")]
    private static string GetTypeKeyword(INamedTypeSymbol type)
        => type.TypeKind switch
        {
            TypeKind.Struct => type.IsRecord ? "record struct" : "struct",
            _ => type.IsRecord ? "record" : "class",
        };

    private static string GetTypeName(INamedTypeSymbol type)
        => type.TypeParameters.Length == 0
            ? type.Name
            : type.Name + "<" + string.Join(", ", type.TypeParameters.Select(static parameter => parameter.Name)) + ">";
}
