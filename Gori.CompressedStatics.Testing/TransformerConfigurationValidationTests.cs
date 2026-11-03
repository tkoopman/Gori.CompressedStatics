using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gori.CompressedStatics.Testing;

public sealed class TransformerConfigurationValidationTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> SelectorCases
        => CreateCases(
        [
            ("Transformer = \"\"", DiagnosticDescriptors.InvalidTransformerFormat),
            ("Transformer = \"   \"", DiagnosticDescriptors.InvalidTransformerFormat),
            ("Transformer = \"::Missing\"", DiagnosticDescriptors.InvalidTransformerFormat),
            ("Transformer = \"Missing::\"", DiagnosticDescriptors.TransformerMemberNotFound),
            ("Transformer = \"Missing::One::Two\", TransformerArguments = null", DiagnosticDescriptors.InvalidTransformerFormat),
            ("Transformer = null", null),
            ("Transformer = \"Unavailable.Transformers.DoesNotExist\"", DiagnosticDescriptors.TransformerTypeNotFound),
            ($"Transformer = {MemberSelector}", null),
            ("Transformer = Selector", DiagnosticDescriptors.TransformerMemberNotFound),
        ]);

    public static TheoryData<MockSourceData> ArgumentCases
        => CreateCases(
        [
            ($"Transformer = {ConstructorSelector}, TransformerArguments = null", null),
            ($"Transformer = {ConstructorSelector}, TransformerArguments = []", null),
            ($"Transformer = {ConstructorSelector}, TransformerArguments = new object[] {{ typeof(Sample) }}", null),
            ($"Transformer = {ConstructorSelector}", null),
            ("TransformerArguments = null", null),
            ("Transformer = null, TransformerArguments = null", null),
            ($"Transformer = {MemberSelector}, TransformerArguments = null", null),
            ($"Transformer = {MemberSelector}, TransformerArguments = []", DiagnosticDescriptors.TransformerArgumentsWithMember),
            ("Transformer = \"Missing::\", TransformerArguments = null", DiagnosticDescriptors.TransformerMemberNotFound),
        ]);

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
                            })
                            { Label = $"{configuration}: {(file ? "file" : "inline")}, {(binary ? "binary" : "string")}, {(tar ? "TAR" : "individual")}" });
                        }
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SelectorCases))]
    public async Task ReportsSelectorSyntaxAndResolutionDiagnostics(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<TransformerAnalyzer>(data, output);

    [Theory]
    [MemberData(nameof(ArgumentCases))]
    public async Task ValidatesArgumentsForResolvableTransformers(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<TransformerAnalyzer>(data, output);

    [Theory]
    [MemberData(nameof(InvalidGeneratorCases))]
    public async Task GeneratorSilentlyClearsInvalidConfiguration(MockSourceData data)
        => await MockProject.AssertGeneratorTest<CompressStaticsGenerator>(data, output);

    private static string ConstructorSelector
        => SymbolDisplay.FormatLiteral(typeof(TransformerGeneratorTests.PrefixStringTransformer).AssemblyQualifiedName!, true);

    private static string MemberSelector
        => SymbolDisplay.FormatLiteral(" " + typeof(TransformerConstructorTests.SelectedProvider).AssemblyQualifiedName + " :: Selected ", true);

    private static TheoryData<MockSourceData> CreateCases((string Configuration, DiagnosticDescriptor? Diagnostic)[] cases)
    {
        var data = new TheoryData<MockSourceData>();
        foreach (string attribute in new[] { "CompressFile", "CompressString" })
        {
            foreach (bool method in new[] { false, true })
            {
                foreach ((string configuration, DiagnosticDescriptor? diagnostic) in cases)
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
                        Expectations = diagnostic is not null
                            ? [new DiagnosticExpectation(diagnostic) { IsExpected = true }, DiagnosticExpectation.NoOthers]
                            : [DiagnosticExpectation.NoOthers],
                    })
                    { Label = $"{attribute} {(method ? "method" : "property")}: {configuration}" });
                }
            }
        }

        return data;
    }
}
