using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.Generators;

namespace Gori.CompressedStatics.Testing;

public sealed class TransformerConfigurationValidationTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> SelectorCases
        => CreateCases(
        [
            ("Transformer = \"\"", true),
            ("Transformer = \"   \"", true),
            ("Transformer = \"::Missing\"", true),
            ("Transformer = \"Missing::\"", true),
            ("Transformer = \"Missing::One::Two\", TransformerArguments = null", true),
            ("Transformer = null", false),
            ("Transformer = \"Missing\"", false),
            ("Transformer = \" Missing, Assembly :: Selected \"", false),
            ("Transformer = Selector", true),
        ], DiagnosticDescriptors.InvalidTransformerFormat);

    public static TheoryData<MockSourceData> ArgumentCases
        => CreateCases(
        [
            ("Transformer = \"Missing\", TransformerArguments = null", true),
            ("Transformer = \"Missing\", TransformerArguments = []", false),
            ("Transformer = \"Missing\", TransformerArguments = new object[] { typeof(Sample) }", false),
            ("Transformer = \"Missing\"", false),
            ("TransformerArguments = null", false),
            ("Transformer = null, TransformerArguments = null", false),
            ("Transformer = \"Missing::Selected\", TransformerArguments = null", false),
            ("Transformer = \"Missing::\", TransformerArguments = null", false),
        ], DiagnosticDescriptors.InvalidTransformerArgument);

    public static TheoryData<MockSourceData> InvalidGeneratorCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            foreach (string configuration in new[]
            {
                "Transformer = \"::Missing\"",
                "Transformer = \"Missing::One::Two\"",
                "Transformer = \"Missing\", TransformerArguments = null",
            })
            {
                foreach (bool binary in new[] { false, true })
                {
                    foreach (bool tar in new[] { false, true })
                    {
                        foreach (bool file in new[] { false, true })
                        {
                            data.Add(new(new MockSourceData
                            {
                                SourceCode = $$"""
                                    using Gori.CompressedStatics;
                                    {{(tar ? "[TarCompress]" : string.Empty)}}
                                    public static partial class Sample
                                    {
                                        [{{(file ? "CompressFile(\"payload.txt\"" : "CompressString(\"must not be transformed\"")}}, {{configuration}})]
                                        public static partial {{(binary ? "byte[]" : "string")}} Value { get; }
                                    }
                                    """,
                                SourcePath = "src\\Sample.cs",
                                AdditionalFilesWithContent = file ? [new("src\\payload.txt", "must not be transformed")] : null,
                                Expectations =
                                [
                                    DiagnosticExpectation.NoOthers,
                                    new FileExpectation { IsExpected = true, Content = tar
                                        ? binary ? "__uncompressed_Value = [];" : "__uncompressed_Value = \"\";"
                                        : binary ? "=> [];" : "=> \"\";" },
                                ],
                            }) { Label = $"{configuration}: {(file ? "file" : "inline")}, {(binary ? "binary" : "string")}, {(tar ? "TAR" : "individual")}" });
                        }
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SelectorCases))]
    public async Task SelectorAnalyzerReportsOnlyMalformedSelectors(MockSourceData data)
        => await MockProject.AssertAnalyserTests<TransformerSelectorAnalyzer>(data, output);

    [Theory]
    [MemberData(nameof(ArgumentCases))]
    public async Task ArgumentAnalyzerValidatesOnlyUsedConstructorArrays(MockSourceData data)
        => await MockProject.AssertAnalyserTests<TransformerArgumentsAnalyzer>(data, output);

    [Theory]
    [MemberData(nameof(InvalidGeneratorCases))]
    public async Task GeneratorSilentlyClearsInvalidConfiguration(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    private static TheoryData<MockSourceData> CreateCases((string Configuration, bool Invalid)[] cases, Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor)
    {
        var data = new TheoryData<MockSourceData>();
        foreach (string attribute in new[] { "CompressFile", "CompressString" })
        {
            foreach (bool method in new[] { false, true })
            {
                foreach ((string configuration, bool invalid) in cases)
                {
                    data.Add(new(new MockSourceData
                    {
                        SourceCode = $$"""
                            using Gori.CompressedStatics;
                            public static partial class Sample
                            {
                                private const string Selector = "Missing::";
                                [{{attribute}}("payload", {{configuration}})]
                                public static partial string Value{{(method ? "();" : " { get; }")}}
                            }
                            """,
                        Expectations = invalid
                            ? [new DiagnosticExpectation(descriptor) { IsExpected = true }, DiagnosticExpectation.NoOthers]
                            : [DiagnosticExpectation.NoOthers],
                    }) { Label = $"{attribute} {(method ? "method" : "property")}: {configuration}" });
                }
            }
        }

        return data;
    }
}
