using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.CompressedStatics.Analyzers;

/// <summary>
/// Analyzes the C# language version used in the compilation and reports a diagnostic if it is lower than C# 13.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CSharpLanguageVersionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.CSharpLanguageVersionMustBe13OrGreater];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        if (context.Compilation is not CSharpCompilation cSharpCompilation)
        {
            return;
        }

        LanguageVersion languageVersion = cSharpCompilation.LanguageVersion;
        if (languageVersion >= LanguageVersion.CSharp13)
        {
            return;
        }

        Location location = cSharpCompilation.SyntaxTrees.FirstOrDefault()?.GetRoot(context.CancellationToken).GetLocation() ?? Location.None;
        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CSharpLanguageVersionMustBe13OrGreater,
                location,
                languageVersion));
    }
}
