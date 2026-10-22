using System.Collections.Immutable;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

        var project = MockProject.Create(
            typeof(CompressFileAttribute).Assembly,
            source,
            languageVersion: languageVersion,
            cancellationToken: TestContext.Current.CancellationToken);

        return await project.GetDiagnosticsAsync<CSharpLanguageVersionAnalyzer>();
    }
}
