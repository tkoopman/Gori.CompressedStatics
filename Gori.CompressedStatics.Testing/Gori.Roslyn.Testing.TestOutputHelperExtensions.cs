#pragma warning disable IDE0130

using System.Collections.Immutable;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace Gori.Roslyn.Testing;

internal static class TestOutputHelperExtensions
{
    public static void Write(this ITestOutputHelper output, IEnumerable<Diagnostic> diagnostics)
    {
        foreach (Diagnostic diagnostic in diagnostics)
        {
            output.WriteLine($"{diagnostic.Severity}: {diagnostic.Id} - {diagnostic.GetMessage(CultureInfo.InvariantCulture)}");
        }
    }

    public static void Write(this ITestOutputHelper output, ImmutableArray<CodeAction> actions)
    {
        foreach (CodeAction action in actions)
        {
            output.WriteLine($"CodeFix Offered: {action.Title}");
        }
    }
}
