using System.Text.RegularExpressions;

using Gori.Common.Roslyn.Test;
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
        => await MockProject.AssertGeneratorTest<AttributeMarkerGenerator>(new MockSourceData
        {
            SourceCode = string.Empty,
            Expectations =
                [
                    new FileExpectation { IsExpected = true, Content = $"internal enum {nameof(CompressionAlgorithm)}" },
                    new FileExpectation { IsExpected = true, Content = $"internal enum {nameof(CompressionCacheMode)}" },
                    new FileExpectation { IsExpected = true, Content = $"internal sealed class {nameof(CompressStringAttribute)}" },
                    new FileExpectation { IsExpected = true, Content = $"internal sealed class {nameof(CompressFileAttribute)}" },
                    new FileExpectation { IsExpected = true, Content = $"internal sealed class {nameof(TarCompressAttribute)}" },
                ],
        }, output);

    [Fact]
    public async Task DecompressStaticsGeneratorTest()
        => await MockProject.AssertGeneratorTest<DecompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = string.Empty,
            Expectations =
                [
                    new FileExpectation { IsExpected = true, Content = $"internal enum {Const.AlgorithmEnumName}" },
                    new FileExpectation { IsExpected = true, Content = $"internal static class {Const.DecompressStaticsClassName}" },
                ],
        }, output);

    [GeneratedRegex(@$"internal enum {Const.AlgorithmEnumName}[\r\n\s]+\{{[\r\n\s]+{nameof(StorageKind.Brotli)},[\r\n\s]+{nameof(StorageKind.GZip)},[\r\n\s]+{nameof(StorageKind.Deflate)},[\r\n\s]+\}}", RegexOptions.Multiline)]
    private static partial Regex AlgorithmEnumRegex();

    [GeneratedRegex(@"\$\{(?<name>.+?)\}")]
    private static partial Regex VariableNamesRegex();
}
