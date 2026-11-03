using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.CodeFix;
using Gori.CompressedStatics.Testing.TestData;

namespace Gori.CompressedStatics.Testing;

public sealed class AnalyzerTests(ITestOutputHelper output)
{
    [Theory]
    [ClassData(typeof(AdditionalFileTestData))]
    public async Task AdditionalFileAnalyserCodeFixTest(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressFileAttributeAdditionalFileAnalyzer, CompressFileAttributeAdditionalFileCodeFixProvider>(data, output);

    [Theory]
    [ClassData(typeof(GeneralCompressAnalyzerTestData))]
    [ClassData(typeof(GenericTypeAnalyzerTestData))]
    [ClassData(typeof(StaticPartialAnalyzerTestData))]
    public async Task CompressAttributeAnalyzerTest(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressAttributeAnalyzer>(data, output);

    [Theory]
    [ClassData(typeof(IgnoredArgumentsAnalyzerTestData))]
    public async Task RemoveIgnoredArgumentCodeFixProviderTest(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressAttributeAnalyzer, RemoveIgnoredArgumentCodeFixProvider>(data, output);

    [Theory]
    [ClassData(typeof(PartialAnalyzerTestData))]
    public async Task AddPartialModifierCodeFixProviderTest(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressAttributeAnalyzer, AddPartialModifierCodeFixProvider>(data, output);

    [Theory]
    [ClassData(typeof(StaticAnalyzerTestData))]
    public async Task AddStaticModifierCodeFixProviderTest(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressAttributeAnalyzer, AddStaticModifierCodeFixProvider>(data, output);

    public static IEnumerable<TheoryDataRow<MockSourceData>> MissingProjectDirectoryCases()
    {
        yield return new TheoryDataRow<MockSourceData>(new MockSourceData
        {
            SourceCode = """
                using Gori.CompressedStatics;

                public static partial class Sample
                {
                    [CompressFile("TestAssets\\AdditionalFiles\\content\\project-root.txt", RelativeToProject = true)]
                    public static partial string Data { get; }
                }
                """,
            SourcePath = "src\\Sample.cs",
            AdditionalFiles = ["TestAssets\\AdditionalFiles\\content\\project-root.txt"],
            Expectations =
            [
                new DiagnosticExpectation(DiagnosticDescriptors.MSBuildProjectDirectoryNotVisible)
                {
                    IsExpected = true,
                    CodeFixExpectations = [CodeFixExpectation.NoOthers],
                },
                DiagnosticExpectation.NoOthers,
            ],
        })
        {
            Label = "No source-relative match: report missing project directory without a code fix",
        };

        yield return new TheoryDataRow<MockSourceData>(new MockSourceData
        {
            SourceCode = """
                using Gori.CompressedStatics;

                public static partial class Sample
                {
                    [CompressFile("Payload.txt", RelativeToProject = true)]
                    public static partial string Data { get; }
                }
                """,
            SourcePath = "src\\Sample.cs",
            AdditionalFilesWithContent = [new("src\\Payload.txt", "payload")],
            Expectations =
            [
                new DiagnosticExpectation(DiagnosticDescriptors.MSBuildProjectDirectoryNotVisible)
                {
                    IsExpected = true,
                    CodeFixExpectations =
                    [
                        new CodeFixExpectation
                        {
                            IsExpected = true,
                            Title = "Remove RelativeToProject",
                            TitleCriteria = MatchCriteria.Exact,
                            EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject",
                            Code = "[CompressFile(\"Payload.txt\")]",
                            CodeCriteria = MatchCriteria.Contains,
                        },
                        CodeFixExpectation.NoOthers,
                    ],
                },
                DiagnosticExpectation.NoOthers,
            ],
        })
        {
            Label = "Source-relative match: offer removal of RelativeToProject",
        };
    }

    [Theory]
    [MemberData(nameof(MissingProjectDirectoryCases))]
    public async Task ReportsOnlyMissingProjectDirectoryDiagnostic(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressFileAttributeAdditionalFileAnalyzer, CompressFileAttributeAdditionalFileCodeFixProvider>(
            data, output, excludeProjectDirectoryProperty: true);
}
