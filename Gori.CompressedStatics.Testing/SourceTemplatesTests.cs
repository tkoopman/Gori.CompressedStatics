using System.Text.RegularExpressions;

using Gori.CompressedStatics.Generators;

namespace Gori.CompressedStatics.Testing;

public partial class SourceTemplatesTests(ITestOutputHelper output)
{
    [Fact]
    public void AttributeMarkers_ShouldContainExpectedContent()
    {
        Assert.NotNull(SourceTemplates.AttributeMarkers);
        Assert.NotEmpty(SourceTemplates.AttributeMarkers);
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
    {
        var project = new MockProject(cancellationToken: TestContext.Current.CancellationToken);
        string results = await project.RunGeneratorDumpAll<AttributeMarkerGenerator>();
        output.WriteLine(results);

        Assert.Contains($"internal enum {nameof(CompressionAlgorithm)}", results);
        Assert.Contains($"internal enum {nameof(CompressionCacheMode)}", results);
        Assert.Contains($"internal sealed class {nameof(CompressStringAttribute)}", results);
        Assert.Contains($"internal sealed class {nameof(CompressFileAttribute)}", results);
        Assert.Contains($"internal sealed class {nameof(TarCompressAttribute)}", results);
    }

    [Fact]
    public async Task DecompressStaticsGeneratorTest()
    {
        var project = new MockProject(cancellationToken: TestContext.Current.CancellationToken);
        string results = await project.RunGeneratorDumpAll<DecompressStaticsGenerator>();
        output.WriteLine(results);

        Assert.Contains($"internal enum {Const.AlgorithmEnumName}", results);
        Assert.Contains($"internal static class {Const.DecompressStaticsClassName}", results);
    }

    [GeneratedRegex(@$"internal enum {Const.AlgorithmEnumName}[\r\n\s]+\{{[\r\n\s]+{nameof(StorageKind.Brotli)},[\r\n\s]+{nameof(StorageKind.GZip)},[\r\n\s]+{nameof(StorageKind.Deflate)},[\r\n\s]+\}}", RegexOptions.Multiline)]
    private static partial Regex AlgorithmEnumRegex();

    [GeneratedRegex(@"\$\{(?<name>.+?)\}")]
    private static partial Regex VariableNamesRegex();
}
