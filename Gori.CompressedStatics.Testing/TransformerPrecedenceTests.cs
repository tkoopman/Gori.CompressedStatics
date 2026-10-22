using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

namespace Gori.CompressedStatics.Testing;

public sealed class TransformerPrecedenceTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> IgnoredArgumentsCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            (string Configuration, string Label)[] cases =
            [
                ("Transformer = \"Base64::Instance\", TransformerArguments = null", "Member ignores null argument array"),
                ("Transformer = \"Base64::Instance\", TransformerArguments = [999]", "Member ignores incompatible constructor arguments"),
                ("TransformerArguments = [999]", "No selector ignores arguments and uses default Base64"),
            ];
            foreach (string attribute in new[] { "CompressFile", "CompressString" })
            {
                foreach ((string configuration, string label) in cases)
                {
                    data.Add(new(new MockSourceData
                    {
                        SourceCode = $$"""
                            using Gori.CompressedStatics;
                            public static partial class Sample
                            {
                                [{{attribute}}("{{(attribute == "CompressFile" ? "payload.txt" : "AQL/")}}", {{configuration}})]
                                public static partial byte[] Value { get; }
                            }
                            """,
                        SourcePath = "src\\Sample.cs",
                        AdditionalFilesWithContent = [new("src\\payload.txt", "AQL/")],
                        Expectations =
                        [
                            DiagnosticExpectation.NoOthers,
                            new FileExpectation { IsExpected = true, Content = "public static partial byte[] Value" },
                            new FileExpectation { IsExpected = true, Content = $"private static readonly byte[] {Const.CacheFieldNamePrefix}Value = [1, 2, 255];" },
                            new FileExpectation { IsExpected = true, Content = $"=> {Const.CacheFieldNamePrefix}Value;" },
                        ],
                    }) { Label = $"{attribute}: {label}" });
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(IgnoredArgumentsCases))]
    public async Task AnalyzerOnlyConfigurationErrorsDoNotChangeGeneratorPrecedence(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);
}
