using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public sealed class MappingIgnoreOverlapTests
{
    public static TheoryData<string, string?, bool> TypeCases
        => [
            new("CS0246", null, true) { Label = "ID-only ignore" },
            new("CS0246", "Missing", true) { Label = "Matching message filter" },
            new("CS0246", "Different", false) { Label = "Nonmatching message filter" },
            new("CS1061", null, false) { Label = "Unrelated diagnostic ID" },
        ];

    [Theory]
    [MemberData(nameof(TypeCases))]
    public async Task ReportsOnlyApplicableIgnores(string diagnosticID, string? filter, bool overlaps)
    {
        const string source = "using System; public class Outer { public class Payload { public Missing Value; } }";
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Context", "Missing")], [new(diagnosticID, filter)]),
        });
        Assert.Contains("global::Destination.Context Value", result);
        Assert.DoesNotContain("using System;", result);
        if (!overlaps)
        {
            Assert.Empty(issues);
            return;
        }

        MappingIgnoreOverlap warning = Assert.IsType<MappingIgnoreOverlap>(Assert.Single(issues));
        Assert.Equal("Missing", warning.Reference);
        Assert.Equal("global::Destination.Context", warning.Destination);
        Assert.Equal("CS0246", warning.DiagnosticID);
        Assert.Equal(filter, warning.MessageContains);
        Assert.NotNull(warning.Location);
        Assert.Equal("Missing", warning.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(warning.Location.SourceSpan));
    }

    [Fact]
    public async Task ReportsExtensionOverlapAndDeduplicatesIdenticalIgnores()
    {
        const string source = """
            using System;
            public class Payload { public object Run(object value) => value.Missing(); }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            RewriteExtensionCalls = true,
            Configuration = RewriterTestConfiguration.Create(
                [new("Destination.Extensions") { ExtensionMethods = [new("Missing")] }], [new("CS1061", "Missing"), new("CS1061", "Missing")]),
        });
        Assert.Contains("global::Destination.Extensions.Missing(value)", result);
        Assert.DoesNotContain("using System;", result);
        MappingIgnoreOverlap warning = Assert.IsType<MappingIgnoreOverlap>(Assert.Single(issues));
        Assert.Equal("Missing", warning.Reference);
        Assert.Equal("global::Destination.Extensions.Missing", warning.Destination);
        Assert.Equal("CS1061", warning.DiagnosticID);
        Assert.Equal("Missing", warning.MessageContains);
    }

    [Fact]
    public async Task IgnoredGenericArgumentIsNotReportedAsMapped()
    {
        const string source = """
            public class Payload { public Missing<Unknown> Value; }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Container", "Missing")], [new("CS0246", "Unknown")]),
        });
        Assert.Empty(issues);
        Assert.Contains("global::Destination.Container<Unknown>", result);
    }

    private static async Task<(string Result, RewriteIssue[] Issues)> Rewrite(string source, CSharpRewriter rewriter)
    {
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        compilation = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Where(tree => tree.GetText(TestContext.Current.CancellationToken).ToString() == source));
        string result = rewriter.Rewrite(source, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
        return (result, [.. issues]);
    }
}
