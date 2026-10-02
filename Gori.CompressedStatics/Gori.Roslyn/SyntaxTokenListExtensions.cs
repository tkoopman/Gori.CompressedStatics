#pragma warning disable IDE0130

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gori.Roslyn;

/// <summary>
/// Provides extension methods for working with SyntaxTokenList.
/// </summary>
internal static class SyntaxTokenListExtensions
{
    private static readonly SyntaxKind[] ModifierOrder =
    [
        SyntaxKind.PublicKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.StaticKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.PartialKeyword,
        SyntaxKind.SealedKeyword,
        SyntaxKind.AbstractKeyword,
        SyntaxKind.VirtualKeyword,
        SyntaxKind.OverrideKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.VolatileKeyword,
        SyntaxKind.AsyncKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.ConstKeyword,
        SyntaxKind.FixedKeyword,
        SyntaxKind.RefKeyword
    ];

    /// <summary>
    /// Inserts a modifier into the given SyntaxTokenList in the correct order according to C# syntax rules.
    /// </summary>
    /// <param name="modifiers">The list of existing modifiers.</param>
    /// <param name="modifierKind">The modifier to insert.</param>
    /// <returns>A new SyntaxTokenList with the modifier inserted in the correct order.</returns>
    public static SyntaxTokenList InsertModifier(this SyntaxTokenList modifiers, SyntaxKind modifierKind)
    {
        // Determine the correct position to insert the modifier based on C# syntax rules
        int insertIndex = 0;

        // Find the index of the modifier to insert in the order array
        int newModifierIndex = Array.IndexOf(ModifierOrder, modifierKind);

        // Iterate through existing modifiers to find the correct insertion point
        for (int i = 0; i < modifiers.Count; i++)
        {
            int existingModifierIndex = Array.IndexOf(ModifierOrder, modifiers[i].Kind());
            if (existingModifierIndex > newModifierIndex)
            {
                insertIndex = i;
                break;
            }

            insertIndex = i + 1; // If we reach here, it means we should insert at the end
        }

        // Insert the new modifier at the determined index
        return modifiers.Insert(insertIndex, SyntaxFactory.Token(modifierKind).WithTrailingTrivia(SyntaxFactory.Space));
    }
}
