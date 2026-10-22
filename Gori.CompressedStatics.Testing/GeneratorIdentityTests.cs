using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

namespace Gori.CompressedStatics.Testing;

public sealed class GeneratorIdentityTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> EscapedIdentifierCases
        => [
            new(new MockSourceData
            {
                SourceCode = """
                    using Gori.CompressedStatics;
                    public static partial class Sample
                    {
                        [CompressString("value")]
                        public static partial string @class { get; }
                    }
                    """,
                Expectations =
                [
                    DiagnosticExpectation.NoOthers,
                    new FileExpectation { IsExpected = true, Content = "public static partial string @class" },
                ],
            }) { Label = "Keyword property name" },
            new(new MockSourceData
            {
                SourceCode = """
                    using Gori.CompressedStatics;
                    public static partial class Sample
                    {
                        [CompressString("value", Algorithm = CompressionAlgorithm.GZip)]
                        public static partial string @return();
                    }
                    """,
                Expectations =
                [
                    DiagnosticExpectation.NoOthers,
                    new FileExpectation { IsExpected = true, Content = "public static partial string @return()" },
                ],
            }) { Label = "Keyword method name with compressed output" },
            new(new MockSourceData
            {
                SourceCode = """
                    using Gori.CompressedStatics;
                    [TarCompress]
                    public static partial class Sample
                    {
                        [CompressString("auto")]
                        public static string @event { get; }
                        [CompressString("partial")]
                        public static partial string @class { get; }
                    }
                    """,
                Expectations =
                [
                    DiagnosticExpectation.NoOthers,
                    new FileExpectation { IsExpected = true, Content = "@event = \"auto\";" },
                    new FileExpectation { IsExpected = true, Content = "public static partial string @class" },
                ],
            }) { Label = "Keyword TAR properties, auto and partial" },
        ];

    [Theory]
    [MemberData(nameof(EscapedIdentifierCases))]
    public async Task KeywordMemberNamesRemainEscaped(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    public static TheoryData<MockSourceData> EscapedEmissionPathCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            foreach (string mode in new[] { "OnInit", "Lazy", "LazyThreadSafe" })
            {
                data.Add(new(new MockSourceData
                {
                    SourceCode = $$"""
                        using Gori.CompressedStatics;
                        public static partial class Sample
                        {
                            [CompressString("value", Algorithm = CompressionAlgorithm.GZip, CacheMode = CompressionCacheMode.{{mode}})]
                            public static partial string @event { get; }
                        }
                        """,
                    Expectations =
                    [
                        DiagnosticExpectation.NoOthers,
                        new FileExpectation { IsExpected = true, Content = "public static partial string @event" },
                        new FileExpectation { IsExpected = true, Content = Const.CacheFieldNamePrefix + "event" },
                        new FileExpectation { IsExpected = false, Content = Const.CacheFieldNamePrefix + "@event" },
                    ],
                }) { Label = $"Compressed keyword property: {mode}" });
            }

            data.Add(new(new MockSourceData
            {
                SourceCode = """
                    using Gori.CompressedStatics;
                    [TarCompress(Algorithm = CompressionAlgorithm.GZip)]
                    public static partial class Sample
                    {
                        static Sample() { }
                        [CompressString("value")]
                        public static partial string @class { get; }
                    }
                    """,
                Expectations =
                [
                    DiagnosticExpectation.NoOthers,
                    new FileExpectation { IsExpected = true, Content = "public static partial string @class => __uncompressed_class;" },
                    new FileExpectation { IsExpected = true, Content = Const.TarBufferMethodName },
                ],
            }) { Label = "Keyword TAR property with existing constructor" });
            data.Add(new(new MockSourceData
            {
                SourceCode = """
                    using Gori.CompressedStatics;
                    public static partial class Sample
                    {
                        [CompressString("value")]
                        public static partial string @return();
                        [CompressString("other")]
                        public static partial string @required { get; }
                    }
                    """,
                Expectations =
                [
                    DiagnosticExpectation.NoOthers,
                    new FileExpectation { IsExpected = true, Content = "public static partial string @return()" },
                    new FileExpectation { IsExpected = true, Content = "public static partial string @required" },
                ],
            }) { Label = "Raw method and contextual-keyword property" });
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EscapedEmissionPathCases))]
    public async Task KeywordEscapingCoversAllEmissionPaths(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    [Fact]
    public async Task DistinctConnectorIdentifiersDoNotCollideInGeneratedHintNames()
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = """
                using Gori.CompressedStatics;
                public static partial class Sample
                {
                    [CompressString("first")]
                    public static partial string A_B { get; }
                    [CompressString("second")]
                    public static partial string A\u203FB { get; }
                }
                """,
            Expectations =
            [
                DiagnosticExpectation.NoOthers,
                new FileExpectation { IsExpected = true, HintName = "Sample.A_u005FB.CompressedStatics.g.cs", Content = "=> \"first\";" },
                new FileExpectation { IsExpected = true, HintName = "Sample.A_u203FB.CompressedStatics.g.cs", Content = "=> \"second\";" },
            ],
        }, output);

    public static TheoryData<MockSourceData> EncodedBackingFieldCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            foreach (int tarKind in new[] { 0, 1, 2 })
            {
                var expectations = new List<KeyedExpectation> { DiagnosticExpectation.NoOthers };
                foreach ((string identifier, string encoded) in new[]
                {
                    ("A_B", "A_u005FB"),
                    ("A\u203FB", "A_u203FB"),
                    ("A_u203FB", "A_u005Fu203FB"),
                    ("A\u0301B", "A_u0301B"),
                })
                {
                    string hint = tarKind == 0 ? $"Sample.{encoded}.CompressedStatics.g.cs" : "Sample-Tar.CompressedStatics.g.cs";
                    expectations.Add(new FileExpectation
                    {
                        IsExpected = true,
                        HintName = hint,
                        Content = $"private static {(tarKind == 0 ? "readonly " : string.Empty)}string {Const.CacheFieldNamePrefix}{encoded}",
                    });
                    expectations.Add(new FileExpectation
                    {
                        IsExpected = true,
                        HintName = hint,
                        Content = $"public static partial string {identifier} => {Const.CacheFieldNamePrefix}{encoded};",
                    });
                }

                data.Add(new(new MockSourceData
                {
                    SourceCode = $$"""
                        using Gori.CompressedStatics;
                        {{(tarKind == 0 ? string.Empty : "[TarCompress(Algorithm = CompressionAlgorithm.GZip)]")}}
                        public static partial class Sample
                        {
                            {{(tarKind == 2 ? "static Sample() { }" : string.Empty)}}
                            [CompressString("first", Algorithm = CompressionAlgorithm.GZip)]
                            public static partial string A_B { get; }
                            [CompressString("second", Algorithm = CompressionAlgorithm.GZip)]
                            public static partial string A\u203FB { get; }
                            [CompressString("third", Algorithm = CompressionAlgorithm.GZip)]
                            public static partial string A_u203FB { get; }
                            [CompressString("fourth", Algorithm = CompressionAlgorithm.GZip)]
                            public static partial string A\u0301B { get; }
                        }
                        """,
                    Expectations = [.. expectations],
                })
                {
                    Label = tarKind switch
                    {
                        0 => "Individual compressed fields and hints",
                        1 => "TAR generated constructor fields",
                        _ => "TAR existing constructor fields",
                    },
                });
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EncodedBackingFieldCases))]
    public async Task EncodedIdentifiersDoNotCollideWithLiteralEscapeSequences(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    public static TheoryData<bool> PartialDeclarationLocations
        => [
            new(false) { Label = "Partial declarations in one file" },
            new(true) { Label = "Partial declarations across files" },
        ];

    [Theory]
    [MemberData(nameof(PartialDeclarationLocations))]
    public async Task TarMembersAcrossPartialDeclarationsRemainInTheirOwnContainingType(bool separateFile)
    {
        const string otherPart = """
            public static partial class Sample
            {
                [Gori.CompressedStatics.CompressString("second")]
                public static partial string Partial { get; }
            }
            """;
        await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(new MockSourceData
        {
            SourceCode = """
                using Gori.CompressedStatics;
                [TarCompress]
                public static partial class Sample
                {
                    [CompressString("first")]
                    public static string Auto { get; }
                }
                [TarCompress]
                public static partial class Other
                {
                    [CompressString("other")]
                    public static partial string Value { get; }
                }
                """ + (separateFile ? string.Empty : Environment.NewLine + otherPart),
            Documents = separateFile ? [new("src\\OtherPart.cs", otherPart)] : [],
            Expectations =
            [
                DiagnosticExpectation.NoOthers,
                new FileExpectation { IsExpected = true, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = "Auto = \"first\";" },
                new FileExpectation { IsExpected = true, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = $"{Const.CacheFieldNamePrefix}Partial = \"second\";" },
                new FileExpectation { IsExpected = false, HintName = "Sample-Tar.CompressedStatics.g.cs", Content = "\"other\"" },
                new FileExpectation { IsExpected = true, HintName = "Other-Tar.CompressedStatics.g.cs", Content = $"{Const.CacheFieldNamePrefix}Value = \"other\";" },
                new FileExpectation { IsExpected = false, HintName = "Other-Tar.CompressedStatics.g.cs", Content = "\"first\"" },
                new FileExpectation { IsExpected = false, HintName = "Other-Tar.CompressedStatics.g.cs", Content = "\"second\"" },
            ],
        }, output);
    }
}
