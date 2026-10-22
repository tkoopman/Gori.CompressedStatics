using System.Collections.Immutable;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public sealed class TransformerAttributeAnalyzerTests
{
    public static TheoryData<string, string, string, string?> ConfigurationCases
    {
        get
        {
            var data = new TheoryData<string, string, string, string?>();
            (string Arguments, string? Diagnostic, string Label)[] cases =
            [
                ("", null, "No transformer configuration"),
                ("Transformer = null", null, "Explicit null selector without arguments"),
                ("TransformerArguments = []", "GCS033", "Arguments without selector"),
                ("Transformer = null, TransformerArguments = []", "GCS033", "Arguments with null selector"),
                ("TransformerArguments = null", "GCS033", "Null arguments still require selector"),
                ("Transformer = \"Missing::Selected\", TransformerArguments = []", "GCS029", "Arguments with static member"),
                ("Transformer = \" Missing, MissingAssembly :: Selected \", TransformerArguments = null", "GCS029", "Padded assembly-qualified member with null arguments"),
                ("Transformer = \"Missing\", TransformerArguments = []", null, "Explicit constructor arguments"),
                ("Transformer = \"Missing\", TransformerArguments = new object[] { 1 }", null, "Explicit array syntax"),
                ("Transformer = \"Missing::Selected\"", null, "Static member without arguments"),
                ("Transformer = \"\", TransformerArguments = []", null, "Malformed selector remains generator-owned"),
                ("Transformer = \"Missing::\", TransformerArguments = []", null, "Missing member remains generator-owned"),
                ("Transformer = \"Missing::One::Two\", TransformerArguments = []", null, "Multiple separators remain generator-owned"),
                ("Transformer = \"Missing\", TransformerArguments = null", null, "Invalid argument array remains generator-owned"),
            ];
            foreach (string attribute in new[] { "CompressFile", "CompressString" })
            {
                foreach (string declaration in new[] { "public static partial string Value { get; }", "public static partial string Value();" })
                {
                    foreach ((string arguments, string? diagnostic, string label) in cases)
                    {
                        data.Add(new(attribute, declaration, arguments, diagnostic)
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
    [MemberData(nameof(ConfigurationCases))]
    public async Task ReportsOnlyInvalidArgumentCombinations(string attribute, string declaration, string arguments, string? expectedId)
    {
        string namedArguments = arguments.Length == 0 ? string.Empty : ", " + arguments;
        var project = MockProject.Create(
            typeof(CompressFileAttribute).Assembly,
            $$"""
            using Gori.CompressedStatics;
            public static partial class Sample
            {
                [{{attribute}}("payload"{{namedArguments}})]
                {{declaration}}
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        ImmutableArray<Diagnostic> diagnostics = await project.GetDiagnosticsAsync<TransformerAttributeAnalyzer>();
        if (expectedId is null)
        {
            Assert.Empty(diagnostics);
            return;
        }

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(expectedId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("'Value'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Equal($"{attribute}(\"payload\"{namedArguments})", diagnostic.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan));
    }
}
