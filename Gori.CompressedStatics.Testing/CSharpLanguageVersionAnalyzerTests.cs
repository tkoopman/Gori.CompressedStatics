using System.Collections.Immutable;

using Gori.CompressedStatics.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.CompressedStatics.Testing;

public sealed class CSharpLanguageVersionAnalyzerTests
{
    public static TheoryData<LanguageVersion> InvalidLanguageVersions
        => [
            new(LanguageVersion.CSharp12) { Label = "C# 12" },
            new(LanguageVersion.CSharp11) { Label = "C# 11" }
        ];

    public static TheoryData<LanguageVersion> ValidLanguageVersions
        => [
            new(LanguageVersion.CSharp13) { Label = "C# 13" },
            new(LanguageVersion.CSharp14) { Label = "C# 14" },
            new(LanguageVersion.Preview) { Label = "Preview" }
        ];

    [Theory]
    [MemberData(nameof(InvalidLanguageVersions))]
    public async Task ReportsDiagnostic_WhenLanguageVersionIsBelow13(LanguageVersion languageVersion)
    {
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(languageVersion);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.CSharpLanguageVersionMustBe13OrGreater.Id, diagnostic.Id);
    }

    [Theory]
    [MemberData(nameof(ValidLanguageVersions))]
    public async Task DoesNotReportDiagnostic_WhenLanguageVersionIs13OrGreater(LanguageVersion languageVersion)
    {
        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(languageVersion);

        Assert.Empty(diagnostics);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(LanguageVersion languageVersion)
    {
        const string source = "public static class Sample { }";

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(languageVersion));

        IEnumerable<PortableExecutableReference> references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        DiagnosticAnalyzer analyzer = new CSharpLanguageVersionAnalyzer();
        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
