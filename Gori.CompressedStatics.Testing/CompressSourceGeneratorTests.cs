using System.Collections.Immutable;
using System.Text;

using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Testing;

public sealed class CompressSourceGeneratorTests(ITestOutputHelper output)
{
    private const string RelativeFileContent = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string RelativeToProjectContent = "root-content-root-content-root-content-root-content-root-content-root-content-root-content-root-content-root-content-root-content-root-content-root-content";

    [Theory]
    [ClassData(typeof(TestData.TarCompressTestData))]
    public async Task GeneratorTests(string source, string[] expectedParts, string[] unexpectedParts)
    {
        var project = new MockProject(source, cancellationToken: TestContext.Current.CancellationToken);
        string generatedText = await project.RunGeneratorDumpAll<CompressStaticsGenerator>();

        output.WriteLine(generatedText);

        foreach (string expectedPart in expectedParts)
        {
            Assert.Contains(expectedPart, generatedText, StringComparison.Ordinal);
        }

        foreach (string unexpectedPart in unexpectedParts)
        {
            Assert.DoesNotContain(unexpectedPart, generatedText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task DoesNotEmitOutput_WhenNoSupportedAttributesAreUsed()
    {
        const string source =
            """
            public static class Sample
            {
                public static string GetText() => "hello";
            }
            """;

        var project = new MockProject(source, cancellationToken: TestContext.Current.CancellationToken);
        string generatedText = await project.RunGeneratorDumpAll<CompressStaticsGenerator>();

        output.WriteLine(generatedText);

        Assert.Empty(generatedText);
    }

    [Fact]
    public async Task EmitsMethodImplementation_WhenCompressStringAttributeIsUsed()
    {
        const string source =
            """
            using Gori.CompressedStatics;

            public static partial class Sample
            {
                [CompressString("hello")]
                public static partial string GetText();
            }
            """;

        var project = new MockProject(source, cancellationToken: TestContext.Current.CancellationToken);
        string generatedText = await project.RunGeneratorDumpAll<CompressStaticsGenerator>();

        output.WriteLine(generatedText);

        Assert.Contains("public static partial string GetText()", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("__cache_", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmitsCompressedPropertyWithLazyCache_WhenCompressionWins()
    {
        const string source =
            """
            using Gori.CompressedStatics;

            public static partial class Sample
            {
                [CompressString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", Algorithm = CompressionAlgorithm.GZip, CacheMode = CompressionCacheMode.Lazy)]
                public static partial string Value { get; }
            }
            """;

        var project = new MockProject(source, cancellationToken: TestContext.Current.CancellationToken);
        string generatedText = await project.RunGeneratorDumpAll<CompressStaticsGenerator>();

        output.WriteLine(generatedText);

        Assert.Contains($"private static readonly byte[] {Const.CompressedFieldNamePrefix}", generatedText, StringComparison.Ordinal);
        Assert.Contains($"private static string {Const.CacheFieldNamePrefix}Value", generatedText, StringComparison.Ordinal);
        Assert.Contains($"??= {Const.DecompressStaticsClassFullName}.AsString({Const.CompressedFieldNamePrefix}", generatedText, StringComparison.Ordinal);
        Assert.Contains($"{Const.AlgorithmEnumFullName}.GZip", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmitsOnLoadCache_WhenConfigured()
    {
        const string source =
            """
            using Gori.CompressedStatics;

            public static partial class Sample
            {
                [CompressString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", Algorithm = CompressionAlgorithm.GZip, CacheMode = CompressionCacheMode.OnLoad)]
                public static partial string Value { get; }
            }
            """;

        var project = new MockProject(source, cancellationToken: TestContext.Current.CancellationToken);
        string generatedText = await project.RunGeneratorDumpAll<CompressStaticsGenerator>();

        output.WriteLine(generatedText);

        Assert.Contains($"private static readonly string {Const.CacheFieldNamePrefix}", generatedText, StringComparison.Ordinal);
        Assert.Contains($"= {Const.DecompressStaticsClassFullName}.AsString({Const.CompressedFieldNamePrefix}", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmitsLazyThreadSafeCache_WhenConfigured()
    {
        const string source =
            """
            using Gori.CompressedStatics;

            public static partial class Sample
            {
                [CompressString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", Algorithm = CompressionAlgorithm.GZip, CacheMode = CompressionCacheMode.LazyThreadSafe)]
                public static partial string Value { get; }
            }
            """;

        var project = new MockProject(source, cancellationToken: TestContext.Current.CancellationToken);
        string generatedText = await project.RunGeneratorDumpAll<CompressStaticsGenerator>();

        output.WriteLine(generatedText);

        Assert.Contains($"private static readonly global::System.Lazy<string> {Const.CacheFieldNamePrefix}", generatedText, StringComparison.Ordinal);
        Assert.Contains($"= new(() => {Const.DecompressStaticsClassFullName}.AsString({Const.CompressedFieldNamePrefix}", generatedText, StringComparison.Ordinal);
        Assert.Contains($"=> {Const.CacheFieldNamePrefix}", generatedText, StringComparison.Ordinal);
        Assert.Contains(".Value;", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmitsFileBackedMember_WhenAdditionalFileIsProvided()
    {
        const string relativeFilePath = "content\\sample.txt";
        string escapedRelativeFilePath = relativeFilePath.Replace("\\", "\\\\", StringComparison.Ordinal);
        string source =
            $$"""
            using Gori.CompressedStatics;

            public static partial class Sample
            {
                [CompressFile("{{escapedRelativeFilePath}}", Algorithm = CompressionAlgorithm.AutoForced)]
                public static partial string FromFile { get; }
            }
            """;

        var project = new MockProject(cancellationToken: TestContext.Current.CancellationToken);

        _ = project.AddAdditionalFile($"src\\{relativeFilePath}", RelativeFileContent)
                   .AddDocument(source, "src\\Sample.cs");

        string generatedText = await project.RunGeneratorDumpAll<CompressStaticsGenerator>();

        output.WriteLine(generatedText);

        Assert.Contains("public static partial string FromFile", generatedText, StringComparison.Ordinal);
        Assert.Contains($"{Const.DecompressStaticsClassFullName}.AsString(", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmitsFileBackedMember_WhenRelativeToProjectIsUsed()
    {
        const string relativeFilePath = "content\\project-root.txt";
        string escapedRelativeFilePath = relativeFilePath.Replace("\\", "\\\\", StringComparison.Ordinal);
        string source =
            $$"""
            using Gori.CompressedStatics;

            public static partial class Sample
            {
                [CompressFile("{{escapedRelativeFilePath}}", RelativeToProject = true, Algorithm = CompressionAlgorithm.AutoForced)]
                public static partial string FromProject { get; }
            }
            """;

        var project = new MockProject(cancellationToken: TestContext.Current.CancellationToken);

        _ = project.AddAdditionalFile(relativeFilePath, RelativeToProjectContent)
                   .AddDocument(source, "src\\Sample.cs");

        string generatedText = await project.RunGeneratorDumpAll<CompressStaticsGenerator>();

        output.WriteLine(generatedText);

        Assert.Contains("public static partial string FromProject", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("// Uncompressed: 0 bytes", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotRecompressUnchangedMember_WhenSiblingMemberChanges()
    {
        const string original =
            """
            using Gori.CompressedStatics;

            public static partial class Sample
            {
                [CompressString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
                public static partial string ValueA { get; }

                [CompressString("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")]
                public static partial string ValueB { get; }
            }
            """;

        const string edited =
            """
            using Gori.CompressedStatics;

            public static partial class Sample
            {
                [CompressString("cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc")]
                public static partial string ValueA { get; }

                [CompressString("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")]
                public static partial string ValueB { get; }
            }
            """;

        var project = new MockProject(cancellationToken: TestContext.Current.CancellationToken);

        Document document = project.AddDocument(original);
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>();
        Compilation compilation = await project.GetCompilationAsync();

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        ImmutableArray<GeneratorRunResult> results = driver.GetRunResult().Results;
        GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);

        IReadOnlyList<(object Value, IncrementalStepRunReason Reason)> outputs =
        [
            .. result.TrackedSteps[CompressStaticsGenerator.IndividualMemberTrackingName]
                .SelectMany(static step => step.Outputs)
        ];

        output.WriteLine(string.Join(", ", outputs.Select(static o => o.Reason)));
        Assert.Equal(2, outputs.Count);
        Assert.All(outputs, static o => Assert.Equal(IncrementalStepRunReason.New, o.Reason));

        document = document.WithText(SourceText.From(edited, Encoding.UTF8));
        compilation = (await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        result = Assert.Single(driver.GetRunResult().Results);

        outputs =
        [
            .. result.TrackedSteps[CompressStaticsGenerator.IndividualMemberTrackingName]
                .SelectMany(static step => step.Outputs)
        ];

        output.WriteLine(string.Join(", ", outputs.Select(static o => o.Reason)));

        Assert.Equal(2, outputs.Count);
        _ = Assert.Single(outputs, static o => o.Reason is IncrementalStepRunReason.Modified);
        _ = Assert.Single(outputs, static o => o.Reason is IncrementalStepRunReason.Cached);
    }
}
