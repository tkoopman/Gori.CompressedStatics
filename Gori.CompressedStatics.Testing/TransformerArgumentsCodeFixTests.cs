using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.CodeFix;

namespace Gori.CompressedStatics.Testing;

public sealed class TransformerArgumentsCodeFixTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> FixCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            (string Configuration, string WithoutArguments, string? WithoutMember, string Label)[] cases =
            [
                ("TransformerArguments = []", "", null, "Missing selector"),
                ("Transformer = null, TransformerArguments = null", "Transformer = null", null, "Null selector and arguments"),
                ("TransformerArguments = new object[] { 1 }, Transformer = \"Example.Provider::Selected\"",
                    "Transformer = \"Example.Provider::Selected\"",
                    "TransformerArguments = new object[] { 1 }, Transformer = \"Example.Provider\"", "Arguments before selector"),
                ("Transformer = \"Example.Provider, Example.Assembly::Selected\", TransformerArguments = []",
                    "Transformer = \"Example.Provider, Example.Assembly::Selected\"",
                    "Transformer = \"Example.Provider, Example.Assembly\", TransformerArguments = []", "Assembly-qualified selector"),
                ("Transformer = @\" Example.Provider :: Selected \", TransformerArguments = null",
                    "Transformer = @\" Example.Provider :: Selected \"",
                    "Transformer = \"Example.Provider\", TransformerArguments = null", "Padded verbatim selector"),
                ("Transformer = \"\"\"Example.Provider::Selected\"\"\", TransformerArguments = [1, \"prefix\"]",
                    "Transformer = \"\"\"Example.Provider::Selected\"\"\"",
                    "Transformer = \"Example.Provider\", TransformerArguments = [1, \"prefix\"]", "Raw literal"),
                ("Transformer = Selector, TransformerArguments = []", "Transformer = Selector",
                    "Transformer = \"Example.Provider, Example.Assembly\", TransformerArguments = []", "Constant selector"),
                ("Transformer = \"Example.Provider\" + \"::Selected\", TransformerArguments = []",
                    "Transformer = \"Example.Provider\" + \"::Selected\"",
                    "Transformer = \"Example.Provider\", TransformerArguments = []", "Concatenated selector"),
            ];
            foreach (string attribute in new[] { "CompressFile", "CompressString" })
            {
                foreach (string declaration in new[] { "public static partial string Value { get; }", "public static partial string Value();" })
                {
                    foreach ((string configuration, string withoutArguments, string? withoutMember, string label) in cases)
                    {
                        var fixes = new List<CodeFixExpectation>
                        {
                            CodeFixExpectation.NotEmpty,
                            new()
                            {
                                IsExpected = true,
                                Title = "Remove TransformerArguments",
                                EquivalenceKey = "TransformerArguments.RemoveArguments",
                                Code = AttributeText(attribute, withoutArguments),
                            },
                            new()
                            {
                                IsExpected = false,
                                EquivalenceKey = "TransformerArguments.RemoveArguments",
                                Code = "TransformerArguments =",
                            },
                        };
                        if (withoutMember is not null)
                        {
                            fixes.Add(new CodeFixExpectation
                            {
                                IsExpected = true,
                                Title = "Remove transformer member selector",
                                EquivalenceKey = "TransformerArguments.RemoveMember",
                                Code = AttributeText(attribute, withoutMember),
                            });
                        }

                        foreach (string key in withoutMember is null
                            ? new[] { "TransformerArguments.RemoveArguments" }
                            : ["TransformerArguments.RemoveArguments", "TransformerArguments.RemoveMember"])
                        {
                            fixes.Add(new CodeFixExpectation
                            {
                                IsExpected = true,
                                EquivalenceKey = key,
                                Code = "private const string Selector = \"Example.Provider, Example.Assembly::Selected\";",
                            });
                            fixes.Add(new CodeFixExpectation { IsExpected = true, EquivalenceKey = key, Code = declaration });
                        }

                        data.Add(new(new MockSourceData
                        {
                            SourceCode = $$"""
                                using Gori.CompressedStatics;
                                public static partial class Sample
                                {
                                    private const string Selector = "Example.Provider, Example.Assembly::Selected";

                                    {{AttributeText(attribute, configuration)}}
                                    {{declaration}}
                                }
                                """,
                            Expectations =
                            [
                                new DiagnosticExpectation(withoutMember is null
                                    ? DiagnosticDescriptors.TransformerArgumentsRequireType
                                    : DiagnosticDescriptors.TransformerArgumentsWithMember)
                                {
                                    IsExpected = true,
                                    CodeFixExpectations = [.. fixes],
                                },
                                DiagnosticExpectation.NoOthers,
                            ],
                        })
                        {
                            Label = $"{attribute} {(declaration.EndsWith(';') ? "method" : "property")}: {label}",
                        });
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(FixCases))]
    public async Task FixesConfigurationWithoutChangingOtherArguments(MockSourceData data)
        => await MockProject.AssertAnalyserTests<TransformerAttributeAnalyzer, TransformerArgumentsCodeFixProvider>(data, output);

    private static string AttributeText(string attribute, string configuration)
        => $"[{attribute}(\"payload\", {(configuration.Length == 0 ? string.Empty : configuration + ", ")}Algorithm = CompressionAlgorithm.GZip)]";
}
