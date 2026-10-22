using System.Collections.Immutable;
using System.Runtime.Loader;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.CodeFix;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Testing;

public sealed class SourceFileTests(ITestOutputHelper output)
{
    private static readonly string SourceContent = "// Original text: \u03bb " + new string('x', 512)
        + "\r\n#if false\r\nThis is inactive, non-C# text.\r\n#endif\r\ninternal class Payload { }\r\n";

    public static TheoryData<string, string, bool> SourcePaths
        => [
            new("Payload.cs", "src\\Payload.cs", false) { Label = "Relative to declaring source" },
            new("content\\Payload.cs", "content\\Payload.cs", true) { Label = "Relative to project" },
            new(MockProject.ProjectDirectory + "\\content\\Payload.cs", "content\\Payload.cs", false) { Label = "Absolute path" },
            new("Payload.CS", "src\\Payload.CS", false) { Label = "Uppercase extension" },
            new("content\\..\\Payload.cs", "src\\Payload.cs", false) { Label = "Normalized path" },
        ];

    public static TheoryData<string?> AdditionalContent
        => [
            new("Additional file wins") { Label = "Nonempty additional file" },
            new(string.Empty) { Label = "Empty additional file" },
            new((string?)null) { Label = "Unavailable additional file" },
        ];

    public static TheoryData<bool, bool> MemberKinds
        => [
            new(false, false) { Label = "Individual property" },
            new(false, true) { Label = "Individual method" },
            new(true, false) { Label = "TAR property" },
            new(true, true) { Label = "Method excluded from TAR" },
        ];

    public static TheoryData<string, bool> MissingFiles
        => [
            new("Missing.cs", false) { Label = "Missing C# file" },
            new("Payload.txt", true) { Label = "Non-C# syntax tree is not a fallback" },
            new("payload.cs", true) { Label = "Path matching remains case-sensitive" },
        ];

    public static TheoryData<bool, string> PathFixes
        => [
            new(false, "Set RelativeToProject = true") { Label = "Toggle to project-relative" },
            new(true, "Remove RelativeToProject") { Label = "Toggle to source-relative" },
            new(false, "Rewrite file path for RelativeToProject = false") { Label = "Rewrite source-relative path" },
            new(true, "Rewrite file path for RelativeToProject = true") { Label = "Rewrite project-relative path" },
        ];

    public static TheoryData<bool> SourceEdits
        => [
            new(false) { Label = "Unrelated source edit is cached" },
            new(true) { Label = "Referenced source edit modifies only its member" },
        ];

    [Theory]
    [MemberData(nameof(SourcePaths))]
    public async Task ResolvesCompiledSource_WithExactOriginalText(string requestedPath, string sourcePath, bool relativeToProject)
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(CreateSource(requestedPath, relativeToProject), "src\\Sample.cs");
        _ = project.AddDocument(SourceContent, sourcePath);

        Assert.Empty(await project.GetDiagnosticsAsync<CompressFileAttributeAdditionalFileAnalyzer>());
        Assert.Equal(SourceContent, await GenerateAndReadValue(project));
    }

    [Theory]
    [MemberData(nameof(MemberKinds))]
    public async Task RoundTripsCompiledSource_ForIndividualAndTarMembers(bool tar, bool method)
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(CreateSource("Payload.cs", tar: tar, method: method), "src\\Sample.cs");
        _ = project.AddDocument(SourceContent, "src\\Payload.cs");

        Assert.Equal(SourceContent, await GenerateAndReadValue(project, method));
    }

    [Theory]
    [MemberData(nameof(AdditionalContent))]
    public async Task AdditionalFileTakesPrecedence_OverCompiledSource(string? additionalContent)
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(CreateSource("Payload.cs"), "src\\Sample.cs");
        _ = project.AddDocument(SourceContent, "src\\Payload.cs");
        GeneratorDriver driver;
        if (additionalContent is null)
        {
            driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
                .AddAdditionalTexts([new UnavailableAdditionalText(Path.Combine(MockProject.ProjectDirectory, "src\\Payload.cs"))]);
        }
        else
        {
            _ = project.AddAdditionalFile("src\\Payload.cs", additionalContent);
            driver = project.CreateGeneratorDriver<CompressStaticsGenerator>();
        }

        driver = driver.RunGenerators(await project.GetCompilationAsync(), TestContext.Current.CancellationToken);
        CompressAttributeInfo member = Assert.Single(GetIndividualOutputs(driver)).Input;
        Assert.Equal(additionalContent, member.SourceValue);
        Assert.Equal(additionalContent ?? string.Empty, await GenerateAndReadValue(project, driver: driver));
    }

    [Theory]
    [MemberData(nameof(MissingFiles))]
    public async Task DoesNotResolveMissingOrIneligibleSource(string requestedPath, bool addSource)
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        if (addSource)
        {
            string sourcePath = requestedPath.EndsWith(".txt", StringComparison.Ordinal) ? requestedPath : "Payload.cs";
            _ = project.AddDocument(SourceContent, "src\\" + sourcePath);
        }

        Document document = project.AddDocument(CreateSource(requestedPath), "src\\Sample.cs");
        Diagnostic diagnostic = Assert.Single(await project.GetDiagnosticsAsync<CompressFileAttributeAdditionalFileAnalyzer>());
        Assert.Equal(DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id, diagnostic.Id);
        Assert.Contains("compiled C# source file (.cs)", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(await project.GetCodeFixActionsAsync<CompressFileAttributeAdditionalFileCodeFixProvider>(document, diagnostic));
        Assert.Equal(string.Empty, await GenerateAndReadValue(project));
    }

    public static TheoryData<bool, bool> InvalidPathCases
        => [
            new(false, false) { Label = "Individual source-relative invalid path" },
            new(true, false) { Label = "Individual project-relative invalid path" },
            new(false, true) { Label = "TAR source-relative invalid path" },
            new(true, true) { Label = "TAR project-relative invalid path" },
        ];

    [Theory]
    [MemberData(nameof(InvalidPathCases))]
    public async Task InvalidPathReportsDiagnosticWithoutCrashingGeneration(bool relativeToProject, bool tar)
    {
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, new MockSourceData
        {
            SourceCode = CreateSource("\0", relativeToProject, tar),
            SourcePath = "src\\Sample.cs",
        }, out _, cancellationToken: TestContext.Current.CancellationToken);
        ImmutableArray<Diagnostic> analyzerDiagnostics = await project.GetDiagnosticsAsync<CompressFileAttributeAdditionalFileAnalyzer>();
        output.Write(analyzerDiagnostics);
        Diagnostic diagnostic = Assert.Single(analyzerDiagnostics);
        Assert.Equal(DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id, diagnostic.Id);

        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGeneratorsAndUpdateCompilation(compilation, out Compilation generatedCompilation, out _, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        output.Write(runResult);
        GeneratorRunResult result = Assert.Single(runResult.Results);
        Assert.Null(result.Exception);
        Assert.Empty(result.Diagnostics);
        string source = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains(tar ? "= \"\";" : "=> \"\";", source, StringComparison.Ordinal);
        Assert.DoesNotContain(Const.DecompressStaticsClassFullName, source, StringComparison.Ordinal);
        Assert.Empty(generatedCompilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(item => item.Severity == DiagnosticSeverity.Error));
        object value = Assert.Single(Assert.Single(result.TrackedSteps[
            tar ? CompressStaticsGenerator.TarTrackingName : CompressStaticsGenerator.IndividualMemberTrackingName]).Outputs).Value;
        CompressAttributeInfo member = tar
            ? Assert.Single(Assert.IsType<(TarMembers Group, Supports Supports)>(value).Group.Members)
            : Assert.IsType<(CompressAttributeInfo Input, Supports Supports)>(value).Input;
        Assert.False(member.IsValid);
        Assert.Null(member.SourceValue);
    }

    [Theory]
    [MemberData(nameof(PathFixes))]
    public async Task PathCodeFixResolvesCompiledSource(bool relativeToProject, string actionTitle)
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(SourceContent, relativeToProject ? "src\\Payload.cs" : "Payload.cs");
        Document document = project.AddDocument(CreateSource("Payload.cs", relativeToProject), "src\\Sample.cs");

        Diagnostic diagnostic = Assert.Single(await project.GetDiagnosticsAsync<CompressFileAttributeAdditionalFileAnalyzer>());
        Assert.Equal(DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id, diagnostic.Id);
        Document? updated = await project.RunSingleCodeFix<CompressFileAttributeAdditionalFileCodeFixProvider>(document, diagnostic, actionTitle);
        Assert.NotNull(updated);
        Compilation compilation = (await updated.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(
            [new CompressFileAttributeAdditionalFileAnalyzer()],
            new AnalyzerOptions([], MockProject.CreateOptionsProvider()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);

        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        Assert.Equal(SourceContent, Assert.Single(GetIndividualOutputs(driver)).Input.SourceValue);
    }

    [Fact]
    public async Task ReportsMissingProjectDirectory_ForCompiledSource()
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(CreateSource("Payload.cs", relativeToProject: true), "src\\Sample.cs");
        _ = project.AddDocument(SourceContent, "Payload.cs");

        ImmutableArray<Diagnostic> diagnostics = await project.GetDiagnosticsAsync<CompressFileAttributeAdditionalFileAnalyzer>(excludeProjectDirectoryProperty: true);
        Assert.Equal(2, diagnostics.Length);
        _ = Assert.Single(diagnostics, d => d.Id == DiagnosticDescriptors.MSBuildProjectDirectoryNotVisible.Id);
        _ = Assert.Single(diagnostics, d => d.Id == DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id);
    }

    [Fact]
    public async Task EmptyCompiledSourceProducesEmptyString()
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(CreateSource("Payload.cs"), "src\\Sample.cs");
        _ = project.AddDocument(string.Empty, "src\\Payload.cs");

        Assert.Empty(await project.GetDiagnosticsAsync<CompressFileAttributeAdditionalFileAnalyzer>());
        Assert.Equal(string.Empty, await GenerateAndReadValue(project));
    }

    [Fact]
    public async Task ByteArrayMemberStillRequiresBase64Content()
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = CreateSource("Payload.cs").Replace("partial string Value", "partial byte[] Value", StringComparison.Ordinal),
            SourcePath = "src\\Sample.cs",
            Documents = [new("src\\Payload.cs", SourceContent)],
            Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.InvalidBase64String) { IsExpected = true },
                    DiagnosticExpectation.NoOthers,
                ],
        }, output);

    [Theory]
    [MemberData(nameof(SourceEdits))]
    public async Task SourceEditsOnlyRecompressAffectedMembers(bool editReferencedSource)
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(
            """
            using Gori.CompressedStatics;
            public static partial class Sample
            {
                [CompressFile("Payload.cs")]
                public static partial string ValueA { get; }
                [CompressFile("Other.cs")]
                public static partial string ValueB { get; }
            }
            """, "src\\Sample.cs");
        _ = project.AddDocument(SourceContent, "src\\Payload.cs");
        _ = project.AddDocument("// Other payload\r\n", "src\\Other.cs");
        _ = project.AddDocument("internal class Unrelated { }", "src\\Unrelated.cs");
        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        Assert.Equal(2, GetIndividualOutputs(driver).Length);

        string editedPath = editReferencedSource ? "Payload.cs" : "Unrelated.cs";
        SyntaxTree tree = compilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == editedPath);
        string updatedText = tree.GetText(TestContext.Current.CancellationToken).ToString() + "\r\n// Edited\r\n";
        compilation = compilation.ReplaceSyntaxTree(tree, tree.WithChangedText(SourceText.From(updatedText)));
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        (CompressAttributeInfo Input, IncrementalStepRunReason Reason)[] outputs = GetIndividualOutputs(driver);
        Assert.Equal(2, outputs.Length);
        (CompressAttributeInfo Input, IncrementalStepRunReason Reason) valueA = Assert.Single(outputs, o => o.Input.MemberName == "ValueA");
        (CompressAttributeInfo Input, IncrementalStepRunReason Reason) valueB = Assert.Single(outputs, o => o.Input.MemberName == "ValueB");
        Assert.Equal(editReferencedSource ? IncrementalStepRunReason.Modified : IncrementalStepRunReason.Cached, valueA.Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, valueB.Reason);
        Assert.Equal(editReferencedSource ? updatedText : SourceContent, valueA.Input.SourceValue);
        Assert.Equal("// Other payload\r\n", valueB.Input.SourceValue);
    }

    private static string CreateSource(string path, bool relativeToProject = false, bool tar = false, bool method = false)
        => $$"""
            using Gori.CompressedStatics;
            {{(tar ? "[TarCompress(Algorithm = CompressionAlgorithm.GZip)]" : string.Empty)}}
            public static partial class Sample
            {
                [CompressFile({{SymbolDisplay.FormatLiteral(path, true)}}, RelativeToProject = {{(relativeToProject ? "true" : "false")}}, Algorithm = CompressionAlgorithm.GZip)]
                public static partial string Value{{(method ? "();" : " { get; }")}}
            }
            """;

    [Fact]
    public async Task ReadsOnlyMatchingAdditionalFile()
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(CreateSource("Payload.cs"), "src\\Sample.cs");
        var matching = new CountingAdditionalText(Path.Combine(MockProject.ProjectDirectory, "src\\Payload.cs"), "Matched content");
        var unrelated = new CountingAdditionalText(Path.Combine(MockProject.ProjectDirectory, "unrelated.txt"), "Unrelated content");
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .AddAdditionalTexts([unrelated, matching])
            .RunGenerators(await project.GetCompilationAsync(), TestContext.Current.CancellationToken);

        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Equal("Matched content", Assert.Single(GetIndividualOutputs(driver)).Input.SourceValue);
        Assert.Equal(1, matching.ReadCount);
        Assert.Equal(0, unrelated.ReadCount);
    }

    private static (CompressAttributeInfo Input, IncrementalStepRunReason Reason)[] GetIndividualOutputs(GeneratorDriver driver)
        => [.. Assert.Single(driver.GetRunResult().Results).TrackedSteps[CompressStaticsGenerator.IndividualMemberTrackingName]
            .SelectMany(step => step.Outputs)
            .Select(o => (Assert.IsType<(CompressAttributeInfo Input, Supports Supports)>(o.Value).Input, o.Reason))];

    private static async Task<string> GenerateAndReadValue(MockProject project, bool method = false, GeneratorDriver? driver = null)
    {
        Compilation compilation = await project.GetCompilationAsync();
        driver ??= project.CreateGeneratorDriver<CompressStaticsGenerator>();
        driver = driver.AddGenerators([new DecompressStaticsGenerator().AsSourceGenerator()])
            .RunGeneratorsAndUpdateCompilation(compilation, out Compilation generatedCompilation, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.All(driver.GetRunResult().Results, result => Assert.Null(result.Exception));

        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult emitResult = generatedCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
        stream.Position = 0;
        var loadContext = new AssemblyLoadContext(nameof(SourceFileTests), isCollectible: true);
        try
        {
            Type? type = loadContext.LoadFromStream(stream).GetType("Sample");
            Assert.NotNull(type);
            object? value = method ? type.GetMethod("Value")!.Invoke(null, null) : type.GetProperty("Value")!.GetValue(null);
            return Assert.IsType<string>(value);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private sealed class CountingAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path => path;

        public int ReadCount { get; private set; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return SourceText.From(content);
        }
    }

    private sealed class UnavailableAdditionalText(string path) : AdditionalText
    {
        public override string Path => path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => null;
    }
}
