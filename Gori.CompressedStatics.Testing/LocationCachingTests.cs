using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Testing;

public sealed class LocationCachingTests
{
    public static TheoryData<bool, bool> MemberCases
        => [
            new(false, false) { Label = "Individual inline member" },
            new(true, false) { Label = "Individual file member" },
            new(false, true) { Label = "TAR inline member" },
            new(true, true) { Label = "TAR file member" },
        ];

    [Theory]
    [MemberData(nameof(MemberCases))]
    public async Task LineInsertionDoesNotRecompressUnchangedOutput(bool file, bool tar)
    {
        MockProject project = CreateProject(file, tar, transformer: false, brotli: false);
        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        string original = Assert.Single(Assert.Single(driver.GetRunResult().Results).GeneratedSources).SourceText.ToString();
        driver = driver.RunGenerators(InsertLines(compilation), TestContext.Current.CancellationToken);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Equal(original, Assert.Single(Assert.Single(driver.GetRunResult().Results).GeneratedSources).SourceText.ToString());
        Assert.Equal(IncrementalStepRunReason.Cached, GetEmissionReason(driver, tar));
    }

    [Theory]
    [MemberData(nameof(MemberCases))]
    public async Task TransformerReceivesCurrentLocationAfterLineInsertion(bool file, bool tar)
    {
        MockProject project = CreateProject(file, tar, transformer: true, brotli: false);
        Compilation compilation = await project.GetCompilationAsync();
        int originalLine = Assert.Single(compilation.SyntaxTrees).GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>()
            .Last().GetLocation().GetLineSpan().StartLinePosition.Line;
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        AssertGeneratedValue(driver, originalLine, tar);
        driver = driver.RunGenerators(InsertLines(compilation), TestContext.Current.CancellationToken);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        AssertGeneratedValue(driver, originalLine + 2, tar);
        Assert.Equal(IncrementalStepRunReason.Modified, GetEmissionReason(driver, tar));
    }

    public static TheoryData<bool> BrotliCases
        => [new(false) { Label = "Individual Brotli diagnostic" }, new(true) { Label = "TAR Brotli diagnostic" }];

    [Theory]
    [MemberData(nameof(BrotliCases))]
    public async Task BrotliDiagnosticMovesWithoutReemitting(bool tar)
    {
        MockProject project = CreateProject(file: false, tar, transformer: false, brotli: true);
        Compilation compilation = await project.GetCompilationAsync();
        compilation = compilation.RemoveReferences(compilation.References.Where(reference =>
            compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
            && assembly.GetTypeByMetadataName(Const.BrotliStreamName21) is not null));
        Assert.True(Supports.GetSupported(compilation).DisableBrotli);
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        Diagnostic original = Assert.Single(driver.GetRunResult().Diagnostics);
        Assert.Equal(DiagnosticDescriptors.BrotliNotAvailable.Id, original.Id);
        string originalSource = Assert.Single(Assert.Single(driver.GetRunResult().Results).GeneratedSources).SourceText.ToString();
        Assert.Contains(tar ? "= \"\";" : "=> \"\";", originalSource, StringComparison.Ordinal);
        driver = driver.RunGenerators(InsertLines(compilation), TestContext.Current.CancellationToken);
        Diagnostic updated = Assert.Single(driver.GetRunResult().Diagnostics);
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.Location.SourceSpan.Start + 4, updated.Location.SourceSpan.Start);
        Assert.Equal(original.Location.GetLineSpan().StartLinePosition.Line + 2, updated.Location.GetLineSpan().StartLinePosition.Line);
        Assert.Equal(IncrementalStepRunReason.Cached, GetEmissionReason(driver, tar));
        Assert.Equal(originalSource, Assert.Single(Assert.Single(driver.GetRunResult().Results).GeneratedSources).SourceText.ToString());
    }

    [Fact]
    public async Task MemberComparersDistinguishPositionsPathsAndContent()
    {
        MockProject project = CreateProject(file: true, tar: false, transformer: false, brotli: false);
        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        CompressAttributeInfo member = GetIndividualMember(driver);
        SyntaxTree original = Assert.Single(compilation.SyntaxTrees);
        SyntaxTree shiftedTree = Assert.Single(InsertLines(compilation).SyntaxTrees);
        CompressAttributeInfo shifted = member with
        {
            Location = Gori.Common.Roslyn.LocationInfo.From(shiftedTree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().Single().GetLocation()),
        };
        Assert.NotEqual(member, shifted);
        Assert.True(CompressAttributeInfo.FilePathComparer.Equals(member, shifted));
        Assert.True(CompressAttributeInfo.EmissionComparer.Equals(member, shifted));
        Assert.Equal(CompressAttributeInfo.FilePathComparer.GetHashCode(member), CompressAttributeInfo.FilePathComparer.GetHashCode(shifted));
        Assert.Equal(CompressAttributeInfo.EmissionComparer.GetHashCode(member), CompressAttributeInfo.EmissionComparer.GetHashCode(shifted));

        SyntaxTree movedTree = original.WithFilePath(Path.Combine(MockProject.ProjectDirectory, "moved\\Sample.cs"));
        CompressAttributeInfo moved = member with
        {
            Location = Gori.Common.Roslyn.LocationInfo.From(movedTree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().Single().GetLocation()),
        };
        Assert.NotEqual(member, moved);
        Assert.False(CompressAttributeInfo.FilePathComparer.Equals(member, moved));
        Assert.True(CompressAttributeInfo.EmissionComparer.Equals(member, moved));
        Assert.False(CompressAttributeInfo.EmissionComparer.Equals(member, member.WithSourceValue("different")));
    }

    [Fact]
    public async Task ResolvedContentStaysPairedWithMembersAfterAddReorderAndRemove()
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddAdditionalFile("src\\a.txt", "a");
        _ = project.AddAdditionalFile("src\\b.txt", "b");
        _ = project.AddAdditionalFile("src\\c.txt", "c");
        _ = project.AddDocument(BuildSource("AB"), "src\\Sample.cs");
        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>();
        foreach (string members in new[] { "AB", "CAB", "BCA", "AC", "C" })
        {
            SyntaxTree tree = Assert.Single(compilation.SyntaxTrees);
            compilation = compilation.ReplaceSyntaxTree(tree, tree.WithChangedText(SourceText.From(BuildSource(members))));
            driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
            GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);
            Assert.Null(result.Exception);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(members.Length, result.GeneratedSources.Length);
            foreach (char member in members)
            {
                GeneratedSourceResult generated = Assert.Single(result.GeneratedSources, source =>
                    source.HintName.Contains("Sample." + member + ".", StringComparison.Ordinal));
                Assert.Contains($"partial string {member}", generated.SourceText.ToString(), StringComparison.Ordinal);
                Assert.Contains($"=> \"{char.ToLowerInvariant(member)}\";", generated.SourceText.ToString(), StringComparison.Ordinal);
            }
        }

        static string BuildSource(string members)
            => "using Gori.CompressedStatics; public static partial class Sample { "
                + string.Join(" ", members.Select(member =>
                    $"[CompressFile(\"{char.ToLowerInvariant(member)}.txt\")] public static partial string {member} {{ get; }}"))
                + " }";
    }

    private static MockProject CreateProject(bool file, bool tar, bool transformer, bool brotli)
    {
        string selector = transformer
            ? $", Transformer = \"{typeof(LocationTransformer).AssemblyQualifiedName}\""
            : string.Empty;
        var data = new MockSourceData
        {
            SourceCode = $$"""
                using Gori.CompressedStatics;
                {{(tar ? $"[TarCompress(Algorithm = CompressionAlgorithm.{(brotli ? "Brotli" : "GZip")})]" : string.Empty)}}
                public static partial class Sample
                {
                    [{{(file ? "CompressFile(\"payload.txt\"" : "CompressString(\"payload\"")}}{{selector}}{{(!tar ? $", Algorithm = CompressionAlgorithm.{(brotli ? "Brotli" : "GZip")}" : string.Empty)}})]
                    public static partial string Value { get; }
                }
                """,
            SourcePath = "src\\Sample.cs",
            AdditionalFilesWithContent = file ? [new("src\\payload.txt", "payload")] : null,
        };
        return MockProject.Create(typeof(CompressFileAttribute).Assembly, data, out _, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static Compilation InsertLines(Compilation compilation)
    {
        SyntaxTree tree = Assert.Single(compilation.SyntaxTrees);
        return compilation.ReplaceSyntaxTree(tree, tree.WithChangedText(SourceText.From("\r\n\r\n" + tree.GetText(TestContext.Current.CancellationToken))));
    }

    private static IncrementalStepRunReason GetEmissionReason(GeneratorDriver driver, bool tar)
        => Assert.Single(Assert.Single(Assert.Single(driver.GetRunResult().Results).TrackedSteps[
            tar ? CompressStaticsGenerator.TarTrackingName : CompressStaticsGenerator.IndividualMemberTrackingName]).Outputs).Reason;

    private static CompressAttributeInfo GetIndividualMember(GeneratorDriver driver)
    {
        object value = Assert.Single(Assert.Single(Assert.Single(driver.GetRunResult().Results)
            .TrackedSteps[CompressStaticsGenerator.IndividualMemberTrackingName]).Outputs).Value;
        return Assert.IsType<(CompressAttributeInfo Input, Supports Supports)>(value).Input;
    }

    private static void AssertGeneratedValue(GeneratorDriver driver, int line, bool tar)
    {
        string source = Assert.Single(Assert.Single(driver.GetRunResult().Results).GeneratedSources).SourceText.ToString();
        Assert.Contains("// Uncompressed: " + (line.ToString(CultureInfo.InvariantCulture).Length * 2).ToString(CultureInfo.InvariantCulture) + " bytes", source, StringComparison.Ordinal);
        object value = Assert.Single(Assert.Single(Assert.Single(driver.GetRunResult().Results).TrackedSteps[
            tar ? CompressStaticsGenerator.TarTrackingName : CompressStaticsGenerator.IndividualMemberTrackingName]).Outputs).Value;
        string? payload = tar
            ? Assert.Single(Assert.IsType<(TarMembers Group, Supports Supports)>(value).Group.Members).SourceValue
            : Assert.IsType<(CompressAttributeInfo Input, Supports Supports)>(value).Input.SourceValue;
        Assert.Equal(line.ToString(CultureInfo.InvariantCulture), payload);
    }

    public sealed class LocationTransformer : IStringTransformer
    {
        public bool TryTransform(ICompressionAttributeInfo attribute, Compilation compilation,
            [NotNullWhen(true)] out string? output, [NotNullWhen(false)] out DiagnosticInfo? diagnostic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = attribute.Location.GetLineSpan().StartLinePosition.Line.ToString(CultureInfo.InvariantCulture);
            diagnostic = null;
            return true;
        }
    }
}
