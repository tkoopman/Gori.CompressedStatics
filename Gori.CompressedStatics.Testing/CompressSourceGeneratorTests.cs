using System.Text;

using Gori.Common.Roslyn.Test;
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
    public async Task GeneratorTests(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

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

        await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = source,
            Expectations = [FileExpectation.NoOthers, DiagnosticExpectation.NoOthers],
        }, output);
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

        await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = source,
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = "public static partial string GetText()" },
                new FileExpectation { IsExpected = false, Content = "__cache_" },
            ],
        }, output);
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

        await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = source,
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = $"private static readonly byte[] {Const.CompressedFieldNamePrefix}" },
                new FileExpectation { IsExpected = true, Content = $"private static string {Const.CacheFieldNamePrefix}Value" },
                new FileExpectation { IsExpected = true, Content = $"??= {Const.DecompressStaticsClassFullName}.AsString({Const.CompressedFieldNamePrefix}" },
                new FileExpectation { IsExpected = true, Content = $"{Const.AlgorithmEnumFullName}.GZip" },
            ],
        }, output);
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

        await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = source,
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = $"private static readonly string {Const.CacheFieldNamePrefix}" },
                new FileExpectation { IsExpected = true, Content = $"= {Const.DecompressStaticsClassFullName}.AsString({Const.CompressedFieldNamePrefix}" },
            ],
        }, output);
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

        await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = source,
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = $"private static readonly global::System.Lazy<string> {Const.CacheFieldNamePrefix}" },
                new FileExpectation { IsExpected = true, Content = $"= new(() => {Const.DecompressStaticsClassFullName}.AsString({Const.CompressedFieldNamePrefix}" },
                new FileExpectation { IsExpected = true, Content = $"=> {Const.CacheFieldNamePrefix}" },
                new FileExpectation { IsExpected = true, Content = ".Value;" },
            ],
        }, output);
    }

    public static TheoryData<string?, bool> RawBinaryCacheCases
    {
        get
        {
            var data = new TheoryData<string?, bool>();
            foreach (string? mode in new string?[] { null, "OnInit", "Lazy", "LazyThreadSafe" })
            {
                foreach (bool empty in new[] { false, true })
                {
                    data.Add(new(mode, empty) { Label = $"{mode ?? "Default"}: {(empty ? "empty" : "nonempty")} raw bytes" });
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(RawBinaryCacheCases))]
    public async Task RawBinaryPropertiesHonorCacheModeAndMethodsRemainUncached(string? mode, bool empty)
    {
        string payload = empty ? string.Empty : "AQID";
        string configuration = mode is null ? string.Empty : $", CacheMode = CompressionCacheMode.{mode}";
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, new MockSourceData
        {
            SourceCode = $$"""
                using Gori.CompressedStatics;
                public static partial class Sample
                {
                    [CompressString("{{payload}}"{{configuration}})]
                    public static partial byte[] @event { get; }
                    [CompressString("{{payload}}")]
                    public static partial byte[] Read();
                }
                """,
        }, out _, cancellationToken: TestContext.Current.CancellationToken);
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGeneratorsAndUpdateCompilation(await project.GetCompilationAsync(), out Compilation compilation, out _, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult driverRunResults = driver.GetRunResult();
        output.Write(driverRunResults);

        GeneratorRunResult result = Assert.Single(driverRunResults.Results);

        Assert.Null(result.Exception);
        Assert.Empty(result.Diagnostics);
        string source = Assert.Single(result.GeneratedSources, item => item.HintName.Contains(".event.", StringComparison.Ordinal)).SourceText.ToString();
        Assert.Contains("// Compression algorithm: Raw", source, StringComparison.Ordinal);
        Assert.DoesNotContain(Const.CompressedFieldNamePrefix, source, StringComparison.Ordinal);
        Assert.DoesNotContain(Const.DecompressStaticsClassFullName, source, StringComparison.Ordinal);
        string literal = empty ? "[]" : "[1, 2, 3]";
        string cacheField = Const.CacheFieldNamePrefix + "event";
        switch (mode)
        {
            case "Lazy":
                Assert.Contains($"=> {cacheField} ??= {literal};", source, StringComparison.Ordinal);
                break;
            case "LazyThreadSafe":
                Assert.Contains($"global::System.Lazy<byte[]> {cacheField} = new(() => {literal});", source, StringComparison.Ordinal);
                Assert.Contains($"=> {cacheField}.Value;", source, StringComparison.Ordinal);
                break;
            default:
                Assert.Contains($"private static readonly byte[] {cacheField} = {literal};", source, StringComparison.Ordinal);
                Assert.Contains($"=> {cacheField};", source, StringComparison.Ordinal);
                break;
        }

        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        stream.Position = 0;
        var loadContext = new System.Runtime.Loader.AssemblyLoadContext(nameof(RawBinaryPropertiesHonorCacheModeAndMethodsRemainUncached), isCollectible: true);
        try
        {
            Type type = Assert.IsType<Type>(loadContext.LoadFromStream(stream).GetType("Sample"), exactMatch: false);
            System.Reflection.PropertyInfo property = Assert.IsType<System.Reflection.PropertyInfo>(type.GetProperty("event"), exactMatch: false);
            System.Reflection.MethodInfo method = Assert.IsType<System.Reflection.MethodInfo>(type.GetMethod("Read"), exactMatch: false);
            byte[] first = Assert.IsType<byte[]>(property.GetValue(null));
            Assert.Equal(empty ? [] : [1, 2, 3], first);
            Assert.Same(first, property.GetValue(null));
            if (!empty)
            {
                first[0] = 99;
                Assert.Equal([99, 2, 3], Assert.IsType<byte[]>(property.GetValue(null)));
                byte[] methodFirst = Assert.IsType<byte[]>(method.Invoke(null, null));
                methodFirst[0] = 42;
                byte[] methodSecond = Assert.IsType<byte[]>(method.Invoke(null, null));
                Assert.NotSame(methodFirst, methodSecond);
                Assert.Equal([1, 2, 3], methodSecond);
            }
            else
            {
                Assert.Empty(Assert.IsType<byte[]>(method.Invoke(null, null)));
            }
        }
        finally
        {
            loadContext.Unload();
        }
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

        await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = source,
            SourcePath = "src\\Sample.cs",
            AdditionalFilesWithContent = [new($"src\\{relativeFilePath}", RelativeFileContent)],
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = "public static partial string FromFile" },
                new FileExpectation { IsExpected = true, Content = $"{Const.DecompressStaticsClassFullName}.AsString(" },
            ],
        }, output);
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

        await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = source,
            SourcePath = "src\\Sample.cs",
            AdditionalFilesWithContent = [new(relativeFilePath, RelativeToProjectContent)],
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = "public static partial string FromProject" },
                new FileExpectation { IsExpected = false, Content = "// Uncompressed: 0 bytes" },
            ],
        }, output);
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

        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);

        Document document = project.AddDocument(original);
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>();
        Compilation compilation = await project.GetCompilationAsync();

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
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
