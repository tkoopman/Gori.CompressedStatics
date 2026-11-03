using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.Generators;

namespace Gori.CompressedStatics.Testing;

public sealed class TarEdgeCaseTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> MixedMemberCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            foreach (bool compressed in new[] { false, true })
            {
                foreach (bool partialFirst in new[] { false, true })
                {
                    foreach (string kind in new[] { "class", "struct" })
                    {
                        string text = compressed ? new string('a', 128) : "a";
                        string firstPartial = partialFirst ? "partial " : string.Empty;
                        string secondPartial = partialFirst ? string.Empty : "partial ";
                        string textTarget = partialFirst ? Const.CacheFieldNamePrefix + "Text" : "Text";
                        string bytesTarget = partialFirst ? "Bytes" : Const.CacheFieldNamePrefix + "Bytes";
                        var expectations = new List<KeyedExpectation>
                        {
                            DiagnosticExpectation.NoOthers,
                            new FileExpectation { IsExpected = true, Content = "static Sample()" },
                            new FileExpectation { IsExpected = false, Content = Const.TarBufferMethodName },
                            new FileExpectation { IsExpected = true, Content = partialFirst
                                ? $"public static partial string Text => {Const.CacheFieldNamePrefix}Text;"
                                : $"public static partial byte[] Bytes => {Const.CacheFieldNamePrefix}Bytes;" },
                            new FileExpectation { IsExpected = false, Content = partialFirst
                                ? "public static partial byte[] Bytes"
                                : "public static partial string Text" },
                            new FileExpectation { IsExpected = true, Content = $"{Const.CacheFieldNamePrefix}Empty = \"\";" },
                            new FileExpectation { IsExpected = true, Content = "EmptyBytes = [];" },
                        };
                        if (compressed)
                        {
                            expectations.Add(new FileExpectation { IsExpected = true, Content = "// Uncompressed: 267 bytes" });
                            expectations.Add(new FileExpectation { IsExpected = true, Content = $"{bytesTarget} = {Const.DecompressStaticsClassFullName}.SegmentAsBytes(tarUncompressed, 0, 3);" });
                            expectations.Add(new FileExpectation { IsExpected = true, Content = $"{Const.CacheFieldNamePrefix}Tail = {Const.DecompressStaticsClassFullName}.SegmentAsString(tarUncompressed, 3, 8);" });
                            expectations.Add(new FileExpectation { IsExpected = true, Content = $"{textTarget} = {Const.DecompressStaticsClassFullName}.SegmentAsString(tarUncompressed, 11, 256);" });
                        }
                        else
                        {
                            expectations.Add(new FileExpectation { IsExpected = true, Content = "// Uncompressed: 13 bytes" });
                            expectations.Add(new FileExpectation { IsExpected = true, Content = $"{textTarget} = \"a\";" });
                            expectations.Add(new FileExpectation { IsExpected = true, Content = $"{bytesTarget} = [1, 2, 255];" });
                            expectations.Add(new FileExpectation { IsExpected = true, Content = $"{Const.CacheFieldNamePrefix}Tail = \"tail\";" });
                            expectations.Add(new FileExpectation { IsExpected = false, Content = Const.TarCompressedFieldName });
                        }

                        data.Add(new(new MockSourceData
                        {
                            SourceCode = $$"""
                                using Gori.CompressedStatics;
                                [TarCompress(Algorithm = CompressionAlgorithm.{{(compressed ? "GZip" : "Auto")}})]
                                public partial {{kind}} Sample
                                {
                                    [CompressString("{{text}}")]
                                    public static {{firstPartial}}string Text { get; }
                                    [CompressString("AQL/")]
                                    public static {{secondPartial}}byte[] Bytes { get; }
                                    [CompressString("")]
                                    public static partial string Empty { get; }
                                    [CompressString("")]
                                    public static byte[] EmptyBytes { get; }
                                    [CompressString("tail")]
                                    public static partial string Tail { get; }
                                }
                                """,
                            Expectations = [.. expectations],
                        })
                        {
                            Label = $"{kind}: {(compressed ? "compressed slices" : "raw literals")}, {(partialFirst ? "partial string / auto binary" : "auto string / partial binary")}",
                        });
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(MixedMemberCases))]
    public async Task MixedPartialAndAutoPropertiesGenerateCorrectAssignments(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    [Theory]
    [MemberData(nameof(MixedMemberCases))]
    public async Task MixedPartialAndAutoPropertiesAreAcceptedByAnalyzer(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressAttributeAnalyzer>(data, output);

    public static TheoryData<MockSourceData> ExistingConstructorCases
        => [
            new(new MockSourceData
            {
                SourceCode = """
                    using Gori.CompressedStatics;
                    [TarCompress(Algorithm = CompressionAlgorithm.GZip)]
                    public static partial class Sample
                    {
                        static Sample() { }
                        [CompressString("")]
                        public static partial string Empty { get; }
                        [CompressString("ab")]
                        public static partial string Text { get; }
                        [CompressString("")]
                        public static partial byte[] EmptyBytes { get; }
                        [CompressString("AQL/")]
                        public static partial byte[] Bytes { get; }
                    }
                    """,
                Expectations =
                [
                    DiagnosticExpectation.NoOthers,
                    new FileExpectation { IsExpected = false, Content = "static Sample()" },
                    new FileExpectation { IsExpected = true, Content = "// Uncompressed: 7 bytes" },
                    new FileExpectation { IsExpected = true, Content = $"Interlocked.Increment(ref {Const.TarBufferUseCountFieldName}) == 2" },
                    new FileExpectation { IsExpected = true, Content = $"{Const.CacheFieldNamePrefix}Empty = \"\";" },
                    new FileExpectation { IsExpected = true, Content = $"{Const.CacheFieldNamePrefix}EmptyBytes = [];" },
                    new FileExpectation { IsExpected = true, Content = $"SegmentAsBytes({Const.TarBufferMethodName}(), 0, 3)" },
                    new FileExpectation { IsExpected = true, Content = $"SegmentAsString({Const.TarBufferMethodName}(), 3, 4)" },
                    new FileExpectation { IsExpected = true, Content = $"{Const.TarBufferFieldName} = null;" },
                ],
            }) { Label = "Empty members do not consume shared-buffer uses or shift non-empty offsets" },
        ];

    [Theory]
    [MemberData(nameof(ExistingConstructorCases))]
    public async Task ExistingConstructorCountsOnlyNonEmptySlices(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    public static TheoryData<MockSourceData> CacheModeCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            foreach (string expression in new[] { "CompressionCacheMode.OnInit", "Mode.OnInit", "OnInit", "(CompressionCacheMode)0", "DefaultMode" })
            {
                foreach (bool hasConstructor in new[] { false, true })
                {
                    data.Add(new(new MockSourceData
                    {
                        SourceCode = $$"""
                            using Gori.CompressedStatics;
                            using Mode = Gori.CompressedStatics.CompressionCacheMode;
                            using static Gori.CompressedStatics.CompressionCacheMode;
                            [TarCompress]
                            public static partial class Sample
                            {
                                private const CompressionCacheMode DefaultMode = CompressionCacheMode.OnInit;
                                {{(hasConstructor ? "static Sample() { }" : string.Empty)}}
                                [CompressString("value", CacheMode = {{expression}}, Algorithm = CompressionAlgorithm.GZip)]
                                public static string Value { get; }
                            }
                            """,
                        Expectations = hasConstructor
                            ? [new DiagnosticExpectation(DiagnosticDescriptors.IgnoreAlgorithmOnTarMember) { IsExpected = true },
                               new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartialWhenTarHasStaticConstructor) { IsExpected = true },
                               DiagnosticExpectation.NoOthers]
                            : [new DiagnosticExpectation(DiagnosticDescriptors.IgnoreAlgorithmOnTarMember) { IsExpected = true }, DiagnosticExpectation.NoOthers],
                    })
                    {
                        Label = $"OnInit via {expression}, {(hasConstructor ? "existing" : "generated")} constructor",
                    });
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(CacheModeCases))]
    public async Task EquivalentCacheModeConstantsHaveIdenticalTarDiagnostics(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressAttributeAnalyzer>(data, output);

    public static TheoryData<MockSourceData> NullExclusionCases
        => [
            new(new MockSourceData
            {
                SourceCode = """
                    using Gori.CompressedStatics;
                    [TarCompress(Exclude = null)]
                    public static partial class Sample
                    {
                        [CompressString("value")]
                        public static string Value { get; }
                    }
                    """,
                Expectations =
                [
                    DiagnosticExpectation.NoOthers,
                    new FileExpectation { IsExpected = true, Content = "static Sample()" },
                    new FileExpectation { IsExpected = true, Content = "Value = \"value\";" },
                ],
            }) { Label = "Null exclusion array excludes no members" },
        ];

    [Theory]
    [MemberData(nameof(NullExclusionCases))]
    public async Task NullExclusionsDoNotCrashGeneration(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    public static TheoryData<MockSourceData> NonDefaultCacheModeCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            foreach (string expression in new[] { "CompressionCacheMode.Lazy", "(CompressionCacheMode)1", "OtherCompressionCacheMode.OnInit" })
            {
                data.Add(new(new MockSourceData
                {
                    SourceCode = $$"""
                        using Gori.CompressedStatics;
                        public static class OtherCompressionCacheMode
                        {
                            public const CompressionCacheMode OnInit = CompressionCacheMode.Lazy;
                        }
                        [TarCompress]
                        public static partial class Sample
                        {
                            [CompressString("value", CacheMode = {{expression}}, Algorithm = CompressionAlgorithm.GZip)]
                            public static string Value { get; }
                        }
                        """,
                    Expectations =
                    [
                        new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartial) { IsExpected = true },
                        DiagnosticExpectation.NoOthers,
                    ],
                })
                { Label = $"Non-default cache mode via {expression} stays outside TAR" });
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(NonDefaultCacheModeCases))]
    public async Task NonDefaultCacheModeIsNotMistakenForTarByItsSpelling(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<CompressAttributeAnalyzer>(data, output);

    [Fact]
    public async Task ExclusionsAndLazyMembersDoNotShiftIncludedTarOffsets()
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = """
                using Gori.CompressedStatics;
                [TarCompress(Algorithm = CompressionAlgorithm.GZip, Exclude = new[] { null, " ", "Excluded", "Excluded" })]
                public static partial class Sample
                {
                    [CompressString("ab")]
                    public static string First { get; }
                    [CompressString("excluded")]
                    public static partial string Excluded { get; }
                    [CompressString("lazy", CacheMode = CompressionCacheMode.Lazy)]
                    public static partial string Lazy { get; }
                    [CompressString("AQL/")]
                    public static partial byte[] Last { get; }
                    [CompressString("method")]
                    public static partial string GetValue();
                }
                """,
            Expectations =
            [
                DiagnosticExpectation.NoOthers,
                new FileExpectation { IsExpected = true, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = "// Uncompressed: 7 bytes" },
                new FileExpectation { IsExpected = true, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = "SegmentAsString(tarUncompressed, 0, 4)" },
                new FileExpectation { IsExpected = true, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = "SegmentAsBytes(tarUncompressed, 4, 3)" },
                new FileExpectation { IsExpected = false, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = "Excluded" },
                new FileExpectation { IsExpected = false, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = "Lazy" },
                new FileExpectation { IsExpected = false, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = "GetValue" },
                new FileExpectation { IsExpected = true, HintName = "Sample.Excluded.CompressedStatics.g.cs", Content = "=> \"excluded\";" },
                new FileExpectation { IsExpected = true, HintName = "Sample.Lazy.CompressedStatics.g.cs", Content = "=> \"lazy\";" },
                new FileExpectation { IsExpected = true, HintName = "Sample.GetValue.CompressedStatics.g.cs", Content = "=> \"method\";" },
            ],
        }, output);
}
