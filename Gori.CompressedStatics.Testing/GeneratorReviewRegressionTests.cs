using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Testing;

public sealed class GeneratorReviewRegressionTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> RecordTarCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            foreach (string kind in new[] { "record", "record struct" })
            {
                foreach ((bool partial, bool constructor) in new[] { (false, false), (true, false), (true, true) })
                {
                    data.Add(new(new MockSourceData
                    {
                        SourceCode = $$"""
                            using Gori.CompressedStatics;
                            [TarCompress]
                            public partial {{kind}} Sample
                            {
                                {{(constructor ? "static Sample() { }" : string.Empty)}}
                                [CompressString("r")]
                                public static {{(partial ? "partial " : string.Empty)}}string Value { get; }
                            }
                            """,
                        Expectations =
                        [
                            DiagnosticExpectation.NoOthers,
                            new FileExpectation { IsExpected = true, Content = $"partial {kind} Sample" },
                            new FileExpectation { IsExpected = true, Content = partial
                                ? "public static partial string Value => __uncompressed_Value;"
                                : "Value = \"r\";" },
                            new FileExpectation { IsExpected = true, Content = partial
                                ? "__uncompressed_Value = \"r\";"
                                : "Value = \"r\";" },
                            new FileExpectation { IsExpected = !constructor, Content = "static Sample()" },
                        ],
                    }) { Label = $"{kind}: {(partial ? "partial property" : "auto-property")}, {(constructor ? "existing constructor" : "generated constructor")}" });
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RecordTarCases))]
    public async Task TarIncludesRecordProperties(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    public static TheoryData<MockSourceData> KeywordTypeCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            foreach (string kind in new[] { "class", "struct" })
            {
                foreach ((string declaration, string expected, bool tar, bool constructor) in new[]
                {
                    ($"public partial {kind} @class", $"partial {kind} @class", false, false),
                    ($"public partial {kind} @class", $"partial {kind} @class", true, false),
                    ($"public partial {kind} @class", $"partial {kind} @class", true, true),
                    ($"public partial class @event {{ public partial {kind} Sample", "partial class @event", false, false),
                    ($"public partial {kind} @class<@event, T>", $"partial {kind} @class<@event, T>", false, false),
                    ($"public partial {kind} @class<@event, T>", $"partial {kind} @class<@event, T>", true, false),
                    ($"public partial {kind} @class<@event, T>", $"partial {kind} @class<@event, T>", true, true),
                    ($"public partial class @event<@class> {{ public partial {kind} Sample", "partial class @event<@class>", false, false),
                })
                {
                    bool nested = declaration.Contains("Sample", StringComparison.Ordinal);
                    data.Add(new(new MockSourceData
                    {
                        SourceCode = $$"""
                            using Gori.CompressedStatics;
                            {{(tar ? "[TarCompress]" : string.Empty)}}
                            {{declaration}}
                            {
                                {{(constructor ? "static @class() { }" : string.Empty)}}
                                [CompressString("keyword payload")]
                                public static partial string Value { get; }
                            }
                            {{(nested ? "}" : string.Empty)}}
                            """,
                        Expectations = tar && !constructor
                            ? [DiagnosticExpectation.NoOthers,
                                new FileExpectation { IsExpected = true, Content = expected },
                                new FileExpectation { IsExpected = true, Content = "static @class()" }]
                            : [DiagnosticExpectation.NoOthers,
                                new FileExpectation { IsExpected = true, Content = expected },
                                new FileExpectation { IsExpected = true, Content = "public static partial string Value" }],
                    }) { Label = $"{kind}: {(declaration.Contains('<') ? "generic " : string.Empty)}{(nested ? "keyword outer type" : tar ? constructor ? "keyword TAR type with existing constructor" : "keyword TAR type with generated constructor" : "keyword containing type")}" });
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(KeywordTypeCases))]
    public async Task KeywordContainingTypeNamesRemainEscaped(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    public static TheoryData<bool> FileMoveCases
        => [new(false) { Label = "Additional-file payload" }, new(true) { Label = "Compiled-source payload" }];

    [Theory]
    [MemberData(nameof(FileMoveCases))]
    public async Task MovingAttributedDocumentResolvesPayloadRelativeToNewDirectory(bool compiledSource)
    {
        string payloadName = compiledSource ? "Payload.cs" : "Payload.txt";
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        Document document = project.AddDocument($$"""
            using Gori.CompressedStatics;
            public static partial class Sample
            {
                [CompressFile("{{payloadName}}")]
                public static partial string Value { get; }
            }
            """, "old\\Sample.cs");
        const string oldPayload = "// Old payload";
        const string newPayload = "// New payload";
        if (compiledSource)
        {
            _ = project.AddDocument(oldPayload, "old\\" + payloadName);
            _ = project.AddDocument(newPayload, "new\\" + payloadName);
        }
        else
        {
            _ = project.AddAdditionalFile("old\\" + payloadName, oldPayload);
            _ = project.AddAdditionalFile("new\\" + payloadName, newPayload);
        }

        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(await project.GetCompilationAsync(), TestContext.Current.CancellationToken);
        AssertGeneratedLiteral(driver, oldPayload);

        Project moved = project.Project.Solution.WithDocumentFilePath(document.Id,
            Path.Combine(MockProject.ProjectDirectory, "new\\Sample.cs")).GetProject(project.Project.Id)!;
        Compilation compilation = (await moved.GetCompilationAsync(TestContext.Current.CancellationToken))!;
        GeneratorDriver fresh = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        AssertGeneratedLiteral(fresh, newPayload);
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        AssertGeneratedLiteral(driver, newPayload);
    }

    public static TheoryData<string, string> DiagnosticMoveCases
        => [
            new("CompressString(\"invalid base64\")", "byte[]") { Label = "Base64 diagnostic" },
            new("CompressString(\"payload\", Transformer = \"MissingTransformerForLocationTest\")", "string") { Label = "Transformer loading diagnostic" },
        ];

    [Theory]
    [MemberData(nameof(DiagnosticMoveCases))]
    public async Task GeneratorDiagnosticFollowsAttributeAfterLineInsertion(string attribute, string returnType)
    {
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, new MockSourceData
        {
            SourceCode = $$"""
                using Gori.CompressedStatics;
                public static partial class Sample
                {
                    [{{attribute}}]
                    public static partial {{returnType}} Value { get; }
                }
                """,
        }, out Document document, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        Diagnostic original = Assert.Single(driver.GetRunResult().Diagnostics);
        SyntaxTree tree = (await document.GetSyntaxTreeAsync(TestContext.Current.CancellationToken))!;
        string source = tree.GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Equal(source.IndexOf(attribute, StringComparison.Ordinal), original.Location.SourceSpan.Start);

        const string prefix = "// Inserted line\r\n\r\n";
        compilation = compilation.ReplaceSyntaxTree(tree, tree.WithChangedText(SourceText.From(prefix + source)));
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        Diagnostic updated = Assert.Single(driver.GetRunResult().Diagnostics);
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.Location.SourceSpan.Start + prefix.Length, updated.Location.SourceSpan.Start);
        Assert.Equal(original.Location.GetLineSpan().StartLinePosition.Line + 2, updated.Location.GetLineSpan().StartLinePosition.Line);
    }

    private static void AssertGeneratedLiteral(GeneratorDriver driver, string expected)
    {
        GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);
        Assert.Null(result.Exception);
        Assert.Empty(result.Diagnostics);
        GeneratedSourceResult generated = Assert.Single(result.GeneratedSources);
        Assert.Contains(SymbolDisplay.FormatLiteral(expected, true), generated.SourceText.ToString(), StringComparison.Ordinal);
    }
}
