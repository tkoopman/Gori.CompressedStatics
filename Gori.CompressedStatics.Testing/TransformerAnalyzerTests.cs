using System.Collections.Immutable;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gori.CompressedStatics.Testing;

public sealed class TransformerAnalyzerTests
{
    public static TheoryData<string, string, string, string?> ConfigurationCases
    {
        get
        {
            var data = new TheoryData<string, string, string, string?>();
            string constructor = SymbolDisplay.FormatLiteral(typeof(TransformerGeneratorTests.PrefixStringTransformer).AssemblyQualifiedName!, true);
            string member = SymbolDisplay.FormatLiteral(typeof(TransformerConstructorTests.SelectedProvider).AssemblyQualifiedName + "::Selected", true);
            string paddedMember = SymbolDisplay.FormatLiteral(" " + typeof(TransformerConstructorTests.SelectedProvider).AssemblyQualifiedName + " :: Selected ", true);
            (string Arguments, string? Diagnostic, string Label)[] cases =
            [
                ("", null, "No transformer configuration"),
                ("Transformer = null", null, "Explicit null selector without arguments"),
                ("TransformerArguments = []", "GCS033", "Arguments without selector"),
                ("Transformer = null, TransformerArguments = []", "GCS033", "Arguments with null selector"),
                ("TransformerArguments = null", null, "Null arguments without selector"),
                ($"Transformer = {member}, TransformerArguments = []", "GCS029", "Arguments with static member"),
                ($"Transformer = {paddedMember}, TransformerArguments = null", null, "Padded assembly-qualified member with null arguments"),
                ($"Transformer = {constructor}, TransformerArguments = []", null, "Explicit constructor arguments"),
                ($"Transformer = {constructor}, TransformerArguments = new object[] {{ 1 }}", null, "Explicit array syntax"),
                ($"Transformer = {member}", null, "Static member without arguments"),
                ("Transformer = \"\", TransformerArguments = []", "GCS016", "Malformed selector"),
                ("Transformer = \"Missing::\", TransformerArguments = null", "GCS023", "Empty member selector"),
                ("Transformer = \"Missing::One::Two\", TransformerArguments = null", "GCS016", "Multiple separators"),
                ($"Transformer = {constructor}, TransformerArguments = null", null, "Null constructor arguments"),
                ("Transformer = \"Unavailable.Transformers.DoesNotExist\"", "GCS017", "Unavailable transformer type"),
                ("Transformer = \"Missing::One::Two\", TransformerArguments = []", "GCS016", "Malformed member selector suppresses argument conflict"),
                ("Transformer = \"Unavailable.Transformers.DoesNotExist::Selected\", TransformerArguments = []", "GCS017", "Missing type suppresses argument conflict"),
                ($"Transformer = {SymbolDisplay.FormatLiteral(typeof(TransformerConstructorTests.SelectedProvider).AssemblyQualifiedName + "::Missing", true)}, TransformerArguments = []", "GCS023", "Missing member suppresses argument conflict"),
                ($"Transformer = {SymbolDisplay.FormatLiteral(typeof(TypeHelperTests.TransformerProvider).AssemblyQualifiedName + "::ObjectField", true)}, TransformerArguments = []", "GCS024", "Incompatible member suppresses argument conflict"),
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
    [MemberData(nameof(TypeHelperTests.OutputClassificationCases), MemberType = typeof(TypeHelperTests))]
    public async Task AcceptsDualOutputTransformerForRequestedReturnType(string selector, ReturnType returnType, string _)
    {
        var project = MockProject.Create(
            typeof(CompressFileAttribute).Assembly,
            $$"""
            using Gori.CompressedStatics;
            public static partial class Sample
            {
                [CompressString("payload", Transformer = {{SymbolDisplay.FormatLiteral(selector, true)}})]
                public static partial {{(returnType == ReturnType.String ? "string" : "byte[]")}} Value { get; }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(await project.GetDiagnosticsAsync<TransformerAnalyzer>());
    }

    [Theory]
    [MemberData(nameof(ConfigurationCases))]
    public async Task ReportsTransformerConfigurationDiagnostics(string attribute, string declaration, string arguments, string? expectedId)
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

        ImmutableArray<Diagnostic> diagnostics = await project.GetDiagnosticsAsync<TransformerAnalyzer>();
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
