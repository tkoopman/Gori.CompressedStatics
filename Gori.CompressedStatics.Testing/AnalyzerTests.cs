using System.Collections.Immutable;

using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.CodeFix;
using Gori.CompressedStatics.Testing.TestData;
using Gori.Roslyn.Testing;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public sealed class AnalyzerTests(ITestOutputHelper output)
{
    [Theory]
    [ClassData(typeof(AdditionalFileTestData))]
    public async Task AdditionalFileAnalyserCodeFixTest(MockSourceData data)
        => await MockProject.AssertAnalyserTests<CompressFileAttributeAdditionalFileAnalyzer, CompressFileAttributeAdditionalFileCodeFixProvider>(data, output);

    [Theory]
    [ClassData(typeof(GeneralCompressAnalyzerTestData))]
    [ClassData(typeof(GenericTypeAnalyzerTestData))]
    [ClassData(typeof(StaticPartialAnalyzerTestData))]
    public async Task CompressAttributeAnalyzerTest(MockSourceData data)
        => await MockProject.AssertAnalyserTests<CompressAttributeAnalyzer>(data, output);

    [Theory]
    [ClassData(typeof(IgnoredArgumentsAnalyzerTestData))]
    public async Task RemoveIgnoredArgumentCodeFixProviderTest(MockSourceData data)
        => await MockProject.AssertAnalyserTests<CompressAttributeAnalyzer, RemoveIgnoredArgumentCodeFixProvider>(data, output);

    [Theory]
    [ClassData(typeof(PartialAnalyzerTestData))]
    public async Task AddPartialModifierCodeFixProviderTest(MockSourceData data)
        => await MockProject.AssertAnalyserTests<CompressAttributeAnalyzer, AddPartialModifierCodeFixProvider>(data, output);

    [Theory]
    [ClassData(typeof(StaticAnalyzerTestData))]
    public async Task AddStaticModifierCodeFixProviderTest(MockSourceData data)
        => await MockProject.AssertAnalyserTests<CompressAttributeAnalyzer, AddStaticModifierCodeFixProvider>(data, output);

    [Fact]
    public async Task ReportsDiagnostic_WhenRelativeToProjectIsUsedWithoutProjectDirectory()
    {
        var project = new MockProject(
            """
            using Gori.CompressedStatics;
            
            public static partial class Sample
            {
                [CompressFile("TestAssets\\AdditionalFiles\\content\\project-root.txt", RelativeToProject = true)]
                public static partial string Data { get; }
            }
            """,
            ["TestAssets\\AdditionalFiles\\content\\project-root.txt"],
            cancellationToken: TestContext.Current.CancellationToken);

        ImmutableArray<Diagnostic> diagnostics = await project.GetDiagnosticsAsync<CompressFileAttributeAdditionalFileAnalyzer>(excludeProjectDirectoryProperty: true);
        output.Write(diagnostics);

        _ = Assert.Single(diagnostics, d => d.Id == DiagnosticDescriptors.MSBuildProjectDirectoryNotVisible.Id);
    }
}
