using System.Diagnostics.CodeAnalysis;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Testing;

public sealed class ProcessingValidityTests
{
    public static TheoryData<bool> MemberKinds
        => [new(false) { Label = "String member" }, new(true) { Label = "Binary member" }];

    [Theory]
    [MemberData(nameof(MemberKinds))]
    public void FailedTransformationWithWarningStillPermanentlyInvalidates(bool binary)
    {
        CompressAttributeInfo member = CreateMember(binary) with { Transformer = typeof(FailingWarningTransformer).AssemblyQualifiedName };
        ProcessingResult<CompressAttributeInfo> result = member.TransformInput(
            Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test"), TestContext.Current.CancellationToken);
        Assert.False(result.Input.IsValid);
        Assert.Null(result.Input.SourceValue);
        Assert.Empty(result.Input.SourceBytes.ToArray());
        Assert.Equal(DiagnosticSeverity.Warning, Assert.IsType<DiagnosticInfo>(result.Diagnostic).Severity);
        Assert.Equal(result.Input, result.Input.WithSourceValue("replacement"));
        Assert.Equal(result.Input, result.Input.WithSourceBytes([1, 2]));
        ProcessingResult<CompressAttributeInfo> repeated = result.Input.TransformInput(
            Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test"), TestContext.Current.CancellationToken);
        Assert.Equal(result.Input, repeated.Input);
        Assert.Null(repeated.Diagnostic);
    }

    [Theory]
    [MemberData(nameof(MemberKinds))]
    public void BrotliValidationClearsPayloadAndReportsOnce(bool binary)
    {
        CompressAttributeInfo member = CreateMember(binary) with { RequestedAlgorithm = CompressionAlgorithm.Brotli };
        if (binary)
        {
            member = member.WithSourceBytes([1, 2, 3]);
        }

        ProcessingResult<CompressAttributeInfo> result = CompressStaticsTransforms.ValidateCompression(member, default);
        Assert.False(result.Input.IsValid);
        Assert.Null(result.Input.SourceValue);
        Assert.Empty(result.Input.SourceBytes.ToArray());
        Assert.Equal(DiagnosticDescriptors.BrotliNotAvailable, Assert.IsType<DiagnosticInfo>(result.Diagnostic).Descriptor);
        ProcessingResult<CompressAttributeInfo> repeated = CompressStaticsTransforms.ValidateCompression(result.Input, default);
        Assert.Equal(result.Input, repeated.Input);
        Assert.Null(repeated.Diagnostic);
        Assert.Equal(result.Input, CompressStaticsTransforms.ValidateCompression(result.Input,
            new Supports { BrotliCompileTime = true, BrotliRuntime = BrotliSupport.Modern }).Input);
    }

    [Theory]
    [MemberData(nameof(MemberKinds))]
    public void InvalidTarGroupReleasesPayloadsRetainsMetadataAndCannotBeRepopulated(bool binary)
    {
        CompressAttributeInfo member = CreateMember(binary);
        if (binary)
        {
            member = member.WithSourceBytes([1, 2, 3]);
        }

        var group = new TarMembers(member.ContainingType, CompressionAlgorithm.Brotli, member.Location, [member]);
        Assert.True(group.IsValid);
        ProcessingResult<TarMembers> result = CompressStaticsTransforms.ValidateCompression(group, default);
        Assert.False(result.Input.IsValid);
        CompressAttributeInfo invalidMember = Assert.Single(result.Input.Members);
        Assert.Equal(member.Invalidate(), invalidMember);
        Assert.False(invalidMember.IsValid);
        Assert.Null(invalidMember.SourceValue);
        Assert.Empty(invalidMember.SourceBytes.ToArray());
        Assert.Equal(DiagnosticDescriptors.BrotliNotAvailable, Assert.IsType<DiagnosticInfo>(result.Diagnostic).Descriptor);
        Assert.Equal(result.Input, result.Input.Invalidate());
        Assert.Equal(result.Input, CompressStaticsTransforms.ResolveTarMembers(result.Input, [member], TestContext.Current.CancellationToken));
        ProcessingResult<TarMembers> repeated = CompressStaticsTransforms.ValidateCompression(result.Input, default);
        Assert.Equal(result.Input, repeated.Input);
        Assert.Null(repeated.Diagnostic);
        Assert.NotEqual(group, result.Input);
        Assert.False(TarMembers.EmissionComparer.Equals(group, result.Input));
        Assert.True(member.IsValid);
        if (binary)
        {
            Assert.Equal(new byte[] { 1, 2, 3 }, member.SourceBytes.ToArray());
        }
        else
        {
            Assert.Equal("payload", member.SourceValue);
        }
    }

    public static TheoryData<bool> EmissionKinds
        => [new(false) { Label = "Individual emission" }, new(true) { Label = "TAR emission" }];

    [Theory]
    [MemberData(nameof(EmissionKinds))]
    public async Task DiagnosticOnlyChangesDoNotRecompress(bool tar)
    {
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, new MockSourceData
        {
            SourceCode = $$"""
                using Gori.CompressedStatics;
                {{(tar ? "[TarCompress]" : string.Empty)}}
                public static partial class Sample
                {
                    [CompressString("Warning", Transformer = "{{typeof(TransformerGeneratorTests.DiagnosticTransformer).AssemblyQualifiedName}}")]
                    public static partial string Value { get; }
                }
                """,
        }, out _, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        Assert.Equal(DiagnosticSeverity.Warning, Assert.Single(driver.GetRunResult().Diagnostics).Severity);
        string original = Assert.Single(Assert.Single(driver.GetRunResult().Results).GeneratedSources).SourceText.ToString();
        SyntaxTree tree = Assert.Single(compilation.SyntaxTrees);
        string updated = tree.GetText(TestContext.Current.CancellationToken).ToString().Replace("\"Warning\"", "\"Info\"", StringComparison.Ordinal);
        compilation = compilation.ReplaceSyntaxTree(tree, tree.WithChangedText(SourceText.From(updated)));
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        Assert.Equal(DiagnosticSeverity.Info, Assert.Single(driver.GetRunResult().Diagnostics).Severity);
        Assert.Equal(original, Assert.Single(Assert.Single(driver.GetRunResult().Results).GeneratedSources).SourceText.ToString());
        Assert.Equal(IncrementalStepRunReason.Cached, Assert.Single(Assert.Single(Assert.Single(driver.GetRunResult().Results)
            .TrackedSteps[tar ? CompressStaticsGenerator.TarTrackingName : CompressStaticsGenerator.IndividualMemberTrackingName]).Outputs).Reason);
    }

    [Theory]
    [MemberData(nameof(MemberKinds))]
    public async Task FailedTransformationBypassesExplicitCompression(bool binary)
    {
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, new MockSourceData
        {
            SourceCode = $$"""
                using Gori.CompressedStatics;
                public static partial class Sample
                {
                    [CompressString("payload", Algorithm = CompressionAlgorithm.GZip, Transformer = "{{typeof(FailingWarningTransformer).AssemblyQualifiedName}}")]
                    public static partial {{(binary ? "byte[]" : "string")}} Value { get; }
                }
                """,
        }, out _, cancellationToken: TestContext.Current.CancellationToken);
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGeneratorsAndUpdateCompilation(await project.GetCompilationAsync(), out Compilation generated, out _, TestContext.Current.CancellationToken);
        GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);
        Assert.Null(result.Exception);
        Assert.Equal(DiagnosticSeverity.Warning, Assert.Single(result.Diagnostics).Severity);
        Assert.Empty(generated.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        string source = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.DoesNotContain("// Compression algorithm", source, StringComparison.Ordinal);
        Assert.Contains(binary ? "=> [];" : "=> \"\";", source, StringComparison.Ordinal);
        Assert.DoesNotContain(Const.DecompressStaticsClassFullName, source, StringComparison.Ordinal);
    }

    public static TheoryData<bool, bool, bool> InvalidEmissionCases
        => [
            new(false, false, false) { Label = "Individual properties and methods" },
            new(true, false, false) { Label = "TAR partial properties, generated constructor" },
            new(true, false, true) { Label = "TAR mixed auto and partial properties" },
            new(true, true, false) { Label = "TAR partial properties, existing constructor" },
        ];

    [Theory]
    [MemberData(nameof(InvalidEmissionCases))]
    public async Task UnavailableBrotliEmitsCompilableEmptyValues(bool tar, bool existingConstructor, bool autoProperties)
    {
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, new MockSourceData
        {
            SourceCode = $$"""
                using Gori.CompressedStatics;
                {{(tar ? "[TarCompress(Algorithm = CompressionAlgorithm.Brotli)]" : string.Empty)}}
                public static partial class Sample
                {
                    {{(existingConstructor ? "static Sample() { }" : string.Empty)}}
                    [CompressString("payload", Algorithm = CompressionAlgorithm.Brotli)]
                    public static {{(autoProperties ? string.Empty : "partial ")}}string Text { get; }
                    [CompressString("AQID", Algorithm = CompressionAlgorithm.Brotli)]
                    public static partial byte[] Bytes { get; }
                    {{(autoProperties ? "[CompressString(\"AQID\")] public static byte[] AutoBytes { get; }" : string.Empty)}}
                    {{(!tar ? "[CompressString(\"payload\", Algorithm = CompressionAlgorithm.Brotli)] public static partial string GetText();" : string.Empty)}}
                    {{(!tar ? "[CompressString(\"AQID\", Algorithm = CompressionAlgorithm.Brotli)] public static partial byte[] GetBytes();" : string.Empty)}}
                }
                """,
        }, out _, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        compilation = compilation.RemoveReferences(compilation.References.Where(reference =>
            compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
            && assembly.GetTypeByMetadataName(Const.BrotliStreamName21) is not null));
        Assert.True(Supports.GetSupported(compilation).DisableBrotli);
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGeneratorsAndUpdateCompilation(compilation, out Compilation generated, out _, TestContext.Current.CancellationToken);
        GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);
        Assert.Null(result.Exception);
        Assert.Equal(tar ? 1 : 4, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(DiagnosticDescriptors.BrotliNotAvailable.Id, diagnostic.Id));
        Assert.Empty(generated.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal(tar ? 1 : 4, result.GeneratedSources.Length);
        string source = string.Join(Environment.NewLine, result.GeneratedSources.Select(item => item.SourceText.ToString()));
        Assert.DoesNotContain(Const.DecompressStaticsClassFullName, source, StringComparison.Ordinal);
        Assert.DoesNotContain("payload", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[1, 2, 3]", source, StringComparison.Ordinal);
        if (tar)
        {
            Assert.Contains($"{(autoProperties ? "Text" : Const.CacheFieldNamePrefix + "Text")} = \"\";", source, StringComparison.Ordinal);
            Assert.Contains($"{Const.CacheFieldNamePrefix}Bytes = [];", source, StringComparison.Ordinal);
            if (autoProperties)
            {
                Assert.Contains("AutoBytes = [];", source, StringComparison.Ordinal);
            }

            Assert.Equal(!existingConstructor, source.Contains("static Sample()", StringComparison.Ordinal));
            Assert.DoesNotContain(Const.TarCompressedFieldName, source, StringComparison.Ordinal);
            Assert.DoesNotContain(Const.TarBufferMethodName, source, StringComparison.Ordinal);
        }
        else
        {
            Assert.Matches("partial string Text\\s*=> \"\";", source);
            Assert.Matches(@"partial byte\[\] Bytes\s*=> \[\];", source);
            Assert.Matches("partial string GetText\\(\\)\\s*=> \"\";", source);
            Assert.Matches(@"partial byte\[\] GetBytes\(\)\s*=> \[\];", source);
        }
    }

    private static CompressAttributeInfo CreateMember(bool binary)
        => new()
        {
            ContainingType = default,
            MemberName = "Value",
            MemberKind = MemberKind.Property,
            Accessibility = "public",
            IsPartial = true,
            ReturnType = binary ? ReturnType.ByteArray : ReturnType.String,
            AttributeType = CompressAttributeType.CompressString,
            SourceValue = "payload",
            Location = default,
        };

    public sealed class FailingWarningTransformer : IStringTransformer, IBinaryTransformer
    {
        public bool TryTransform(ICompressionAttributeInfo attribute, Compilation compilation,
            [NotNullWhen(true)] out string? output, [NotNullWhen(false)] out DiagnosticInfo? diagnostic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = "ignored output";
            diagnostic = new DiagnosticInfo(TransformerGeneratorTests.DiagnosticTransformer.Descriptor);
            return false;
        }

        public bool TryTransform(ICompressionAttributeInfo attribute, Compilation compilation,
            [NotNullWhen(true)] out byte[]? output, [NotNullWhen(false)] out DiagnosticInfo? diagnostic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = [1, 2, 3];
            diagnostic = new DiagnosticInfo(TransformerGeneratorTests.DiagnosticTransformer.Descriptor);
            return false;
        }
    }
}
