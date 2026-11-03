using System.Collections.Immutable;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Analyzers;
using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public sealed class CSharpRewriterConfigurationAnalyzerTests
{
    [Fact]
    public async Task ReportsOnlyAdvisoryConfigurationDiagnostics()
    {
        const string source = """
            using Gori.CompressedStatics;

            [assembly: CSharpRewriterIgnore("CS0656")]
            [assembly: CSharpRewriterIgnore("not-a-diagnostic")]
            [assembly: CSharpRewriterTypeMapping("__CSharpRewriterPlaceholder__.Context")]
            [assembly: CSharpRewriterTypeMapping("Library.Extensions", ExtensionMethods = ["Transform(*, string, ...)"])]
            [assembly: CSharpRewriterTypeMapping("Library.Extensions", ExtensionMethods = ["Malformed("])]
            [assembly: CSharpRewriterTypeMapping("Library.FirstContext", "Old.Context")]
            [assembly: CSharpRewriterTypeMapping("Library.SecondContext", "Old.Context")]

            public class Sample { }
            """;
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);

        ImmutableArray<Diagnostic> diagnostics = await project.GetDiagnosticsAsync<CSharpRewriterConfigurationAnalyzer>();

        Assert.Equal(3, diagnostics.Length);
        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Id == DiagnosticDescriptors.CSharpRewriterBroadIgnore.Id));
        Diagnostic placeholder = Assert.Single(diagnostics, diagnostic => diagnostic.Id == DiagnosticDescriptors.CSharpRewriterMappingPlaceholder.Id);
        Assert.Equal(DiagnosticSeverity.Error, placeholder.Severity);
        Assert.Equal("\"__CSharpRewriterPlaceholder__.Context\"", source.Substring(placeholder.Location.SourceSpan.Start, placeholder.Location.SourceSpan.Length));
        Assert.All(diagnostics.Where(diagnostic => diagnostic.Id == DiagnosticDescriptors.CSharpRewriterBroadIgnore.Id), diagnostic =>
        {
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.StartsWith("CSharpRewriterIgnore(", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
        });
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == DiagnosticDescriptors.CSharpRewriterInvalidConfiguration.Id);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == DiagnosticDescriptors.CSharpRewriterConflictingConfiguration.Id);
    }
}
