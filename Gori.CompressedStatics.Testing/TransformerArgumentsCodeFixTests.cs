using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.CodeFix;

using Microsoft.CodeAnalysis.CSharp;

namespace Gori.CompressedStatics.Testing;

public sealed class TransformerArgumentsCodeFixTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> FixCases
    {
        get
        {
            var data = new TheoryData<MockSourceData>();
            string typeName = typeof(CodeFixTransformer).FullName!;
            string typeLiteral = SymbolDisplay.FormatLiteral(typeName, true);
            string memberLiteral = SymbolDisplay.FormatLiteral(typeName + "::Selected", true);
            string qualifiedType = SymbolDisplay.FormatLiteral(typeof(CodeFixTransformer).AssemblyQualifiedName!, true);
            string qualifiedMember = SymbolDisplay.FormatLiteral(typeof(CodeFixTransformer).AssemblyQualifiedName + "::Selected", true);
            string constantDeclaration = $"private const string Selector = {qualifiedMember};";
            (string Configuration, string WithoutArguments, string? WithoutMember, string Label)[] cases =
            [
                ("TransformerArguments = []", "", null, "Missing selector"),
                ("Transformer = null, TransformerArguments = []", "Transformer = null", null, "Null selector with arguments"),
                ($"TransformerArguments = new object[] {{ 1 }}, Transformer = {memberLiteral}",
                    $"Transformer = {memberLiteral}",
                    $"TransformerArguments = new object[] {{ 1 }}, Transformer = {typeLiteral}", "Arguments before selector"),
                ($"Transformer = {qualifiedMember}, TransformerArguments = []",
                    $"Transformer = {qualifiedMember}",
                    $"Transformer = {qualifiedType}, TransformerArguments = []", "Assembly-qualified selector"),
                ($"Transformer = @\" {typeName} :: Selected \", TransformerArguments = []",
                    $"Transformer = @\" {typeName} :: Selected \"",
                    $"Transformer = {typeLiteral}, TransformerArguments = []", "Padded verbatim selector"),
                ($"Transformer = \"\"\"{typeName}::Selected\"\"\", TransformerArguments = [1, \"prefix\"]",
                    $"Transformer = \"\"\"{typeName}::Selected\"\"\"",
                    $"Transformer = {typeLiteral}, TransformerArguments = [1, \"prefix\"]", "Raw literal"),
                ("Transformer = Selector, TransformerArguments = []", "Transformer = Selector",
                    $"Transformer = {qualifiedType}, TransformerArguments = []", "Constant selector"),
                ($"Transformer = {typeLiteral} + \"::Selected\", TransformerArguments = []",
                    $"Transformer = {typeLiteral} + \"::Selected\"",
                    $"Transformer = {typeLiteral}, TransformerArguments = []", "Concatenated selector"),
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
                                Code = constantDeclaration,
                            });
                            fixes.Add(new CodeFixExpectation { IsExpected = true, EquivalenceKey = key, Code = declaration });
                        }

                        data.Add(new(new MockSourceData
                        {
                            SourceCode = $$"""
                                using Gori.CompressedStatics;
                                public static partial class Sample
                                {
                                    {{constantDeclaration}}

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
        => await MockProject.AssertAnalyzerTests<TransformerAnalyzer, TransformerArgumentsCodeFixProvider>(data, output);

    public static TheoryData<MockSourceData> NullArgumentCases
    {
        get
        {
            TheoryData<MockSourceData> data = [];
            string member = SymbolDisplay.FormatLiteral(typeof(CodeFixTransformer).AssemblyQualifiedName + "::Selected", true);
            foreach (string attribute in new[] { "CompressFile", "CompressString" })
            {
                foreach (bool method in new[] { false, true })
                {
                    foreach (string configuration in new[] { "TransformerArguments = null", "Transformer = null, TransformerArguments = null", $"Transformer = {member}, TransformerArguments = null" })
                    {
                        data.Add(new(new MockSourceData
                        {
                            SourceCode = $$"""
                                using Gori.CompressedStatics;
                                public static partial class Sample
                                {
                                    {{AttributeText(attribute, configuration)}}
                                    public static partial string Value{{(method ? "();" : " { get; }")}}
                                }
                                """,
                            Expectations = [DiagnosticExpectation.NoOthers],
                        })
                        { Label = $"{attribute} {(method ? "method" : "property")}: {configuration}" });
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(NullArgumentCases))]
    public async Task NullArgumentsDoNotRequireCodeFix(MockSourceData data)
        => await MockProject.AssertAnalyzerTests<TransformerAnalyzer, TransformerArgumentsCodeFixProvider>(data, output);

    public sealed class CodeFixTransformer : TypeHelperTests.StringTransformer
    {
        public CodeFixTransformer()
        {
        }

        public CodeFixTransformer(int value)
        {
        }

        public CodeFixTransformer(int value, string prefix)
        {
        }

        public static CodeFixTransformer Selected { get; } = new();
    }

    private static string AttributeText(string attribute, string configuration)
        => $"[{attribute}(\"payload\", {(configuration.Length == 0 ? string.Empty : configuration + ", ")}Algorithm = CompressionAlgorithm.GZip)]";
}
