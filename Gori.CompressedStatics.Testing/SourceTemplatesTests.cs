using System.Text.RegularExpressions;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public partial class SourceTemplatesTests(ITestOutputHelper output)
{
    [Fact]
    public void AttributeTemplates_ShouldContainExpectedContent()
    {
        string[] templates =
        [
            SourceTemplates.CompressionAlgorithm,
            SourceTemplates.CompressionCacheMode,
            SourceTemplates.CompressStringAttribute,
            SourceTemplates.CompressFileAttribute,
            SourceTemplates.TarCompressAttribute,
        ];

        Assert.All(templates, Assert.NotEmpty);
    }

    /// <summary>
    /// Tests that the DecompressStatics template contains the expected content.
    /// Mainly to confirm content aligns with constants defined in Const.cs and StorageKind enum.
    /// </summary>
    [Fact]
    public void DecompressStatics_ShouldContainExpectedContent()
    {
        string actualContent = SourceTemplates.DecompressStatics;

        // Namespace
        Assert.Contains($"namespace {Const.DecompressStaticsNamespace}", actualContent);

        // Algorithm enum
        Assert.Matches(AlgorithmEnumRegex(), actualContent);

        // Check all variable names in the template are valid
        var variables = VariableNamesRegex().Matches(actualContent)
            .Select(m => m.Groups["name"].Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(variables);
        Assert.All(variables, name => Assert.Equal(Const.DecompressStaticsBrotliVariableName, name));

        // Class
        Assert.Contains($"internal static class {Const.DecompressStaticsClassName}", actualContent);

        // Algorithm references
        Assert.Equal(2, Regex.Count(actualContent, Regex.Escape($"{Const.AlgorithmEnumFullName}.{nameof(StorageKind.Brotli)}")));
        Assert.Equal(2, Regex.Count(actualContent, Regex.Escape($"{Const.AlgorithmEnumFullName}.{nameof(StorageKind.GZip)}")));
        Assert.Equal(2, Regex.Count(actualContent, Regex.Escape($"{Const.AlgorithmEnumFullName}.{nameof(StorageKind.Deflate)}")));
    }

    [Fact]
    public async Task AttributeMarkerGeneratorTest()
        => await MockProject.AssertGeneratorTest<AttributesGenerator>(new MockSourceData
        {
            SourceCode = string.Empty,
            Expectations =
                [
                    FileExpectation.NoOthers,
                    new FileExpectation { IsExpected = true, HintName = "Microsoft.CodeAnalysis.EmbeddedAttribute.cs" },
                    new FileExpectation { IsExpected = true, HintName = "Gori.CompressedStatics.CompressionAlgorithm.g.cs", Content = $"internal enum {nameof(CompressionAlgorithm)}" },
                    new FileExpectation { IsExpected = true, HintName = "Gori.CompressedStatics.CompressionCacheMode.g.cs", Content = $"internal enum {nameof(CompressionCacheMode)}" },
                    new FileExpectation { IsExpected = true, HintName = "Gori.CompressedStatics.CompressStringAttribute.g.cs", Content = $"internal sealed class {nameof(CompressStringAttribute)}" },
                    new FileExpectation { IsExpected = true, HintName = "Gori.CompressedStatics.CompressFileAttribute.g.cs", Content = $"internal sealed class {nameof(CompressFileAttribute)}" },
                    new FileExpectation { IsExpected = true, HintName = "Gori.CompressedStatics.TarCompressAttribute.g.cs", Content = $"internal sealed class {nameof(TarCompressAttribute)}" },
                    new FileExpectation { IsExpected = true, HintName = "Gori.CompressedStatics.CSharpRewriterOptions.g.cs", Content = $"internal enum {nameof(CSharpRewriterOptions)}" },
                    new FileExpectation { IsExpected = true, HintName = "Gori.CompressedStatics.CSharpRewriterIgnoreAttribute.g.cs", Content = $"internal sealed class {nameof(CSharpRewriterIgnoreAttribute)}" },
                    new FileExpectation { IsExpected = true, HintName = "Gori.CompressedStatics.CSharpRewriterTypeMappingAttribute.g.cs", Content = $"internal sealed class {nameof(CSharpRewriterTypeMappingAttribute)}" },
                ],
        }, output);

    [Fact]
    public async Task AttributeMarkerGeneratorAddsHeadersEmbeddedAttributesAndNullableDirectives()
    {
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, string.Empty,
            cancellationToken: TestContext.Current.CancellationToken);

        GeneratorDriver driver = project.CreateGeneratorDriver<AttributesGenerator>()
            .RunGeneratorsAndUpdateCompilation(await project.GetCompilationAsync(), out _,
                out _, TestContext.Current.CancellationToken);

        GeneratedSourceResult[] generatedSources = [.. Assert.Single(driver.GetRunResult().Results).GeneratedSources
            .Where(source => source.HintName != "Microsoft.CodeAnalysis.EmbeddedAttribute.cs")];

        foreach (GeneratedSourceResult source in generatedSources)
        {
            output.Write(source);
        }

        Assert.Equal(8, generatedSources.Length);
        Assert.All(generatedSources, source =>
        {
            string content = source.SourceText.ToString();
            Assert.StartsWith(Const.AutoGeneratedHeader, content);
            Assert.Contains("[global::Microsoft.CodeAnalysis.Embedded]", content);
        });

        Assert.Contains(generatedSources, source => source.HintName == "Gori.CompressedStatics.CompressStringAttribute.g.cs"
            && source.SourceText.ToString().Contains(Const.NullableDirective, StringComparison.Ordinal));
    }

    [Fact]
    public async Task EmittedRewriterAttributesUseAssemblyOnlyTargetsInRootNamespace()
    {
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, string.Empty,
            cancellationToken: TestContext.Current.CancellationToken);

        GeneratorDriver driver = project.CreateGeneratorDriver<AttributesGenerator>()
            .RunGeneratorsAndUpdateCompilation(await project.GetCompilationAsync(), out Compilation compilation,
                out _, TestContext.Current.CancellationToken);

        foreach (GeneratedSourceResult source in driver.GetRunResult().Results.SelectMany(result => result.GeneratedSources))
        {
            output.Write(source);
        }

        Assert.Null(Assert.Single(driver.GetRunResult().Results).Exception);
        foreach (string name in new[] { nameof(CSharpRewriterIgnoreAttribute), nameof(CSharpRewriterTypeMappingAttribute) })
        {
            INamedTypeSymbol attribute = Assert.IsType<INamedTypeSymbol>(compilation.GetTypeByMetadataName("Gori.CompressedStatics." + name), exactMatch: false);
            Assert.All(attribute.Locations, location => Assert.True(location.IsInSource));
            AttributeData usage = Assert.Single(attribute.GetAttributes(), candidate => candidate.AttributeClass?.Name == nameof(AttributeUsageAttribute));
            Assert.Equal((int)AttributeTargets.Assembly, usage.ConstructorArguments[0].Value);
            Assert.Equal(true, Assert.Single(usage.NamedArguments, argument => argument.Key == nameof(AttributeUsageAttribute.AllowMultiple)).Value.Value);
            Assert.Equal(false, Assert.Single(usage.NamedArguments, argument => argument.Key == nameof(AttributeUsageAttribute.Inherited)).Value.Value);
            Assert.Null(compilation.GetTypeByMetadataName("Gori.CompressedStatics.CSharp." + name));
        }
    }

    [Fact]
    public async Task DecompressStaticsGeneratorTest()
        => await MockProject.AssertGeneratorTest<DecompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = string.Empty,
            Expectations =
                [
                    FileExpectation.NoOthers,
                    DiagnosticExpectation.NoOthers,
                    new FileExpectation { IsExpected = true, Content = $"internal enum {Const.AlgorithmEnumName}" },
                    new FileExpectation { IsExpected = true, Content = $"internal static class {Const.DecompressStaticsClassName}" },
                ],
        }, output);

    [GeneratedRegex(@$"internal enum {Const.AlgorithmEnumName}[\r\n\s]+\{{[\r\n\s]+{nameof(StorageKind.Brotli)},[\r\n\s]+{nameof(StorageKind.GZip)},[\r\n\s]+{nameof(StorageKind.Deflate)},[\r\n\s]+\}}", RegexOptions.Multiline)]
    private static partial Regex AlgorithmEnumRegex();

    [GeneratedRegex(@"\$\{(?<name>.+?)\}")]
    private static partial Regex VariableNamesRegex();
}
