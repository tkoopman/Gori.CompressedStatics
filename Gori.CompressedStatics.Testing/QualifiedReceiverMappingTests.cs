using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gori.CompressedStatics.Testing;

public sealed class QualifiedReceiverMappingTests
{
    public static TheoryData<string, string> Expressions
        => [
            new("Old.Context.Create()", "global::Destination.Context.Create()") { Label = "Static call" },
            new("Old.Context.Current", "global::Destination.Context.Current") { Label = "Static property" },
            new("Old.Context.Create<int>()", "global::Destination.Context.Create<int>()") { Label = "Generic method" },
            new("Old.Context<int>.Create()", "global::Destination.Context<int>.Create()") { Label = "Generic receiver" },
            new("Old.Context.Nested.Create()", "global::Destination.Context.Nested.Create()") { Label = "Nested type" },
        ];

    [Theory]
    [MemberData(nameof(Expressions))]
    public async Task MapsQualifiedReceivers(string expression, string expected)
    {
        string source = $$"""
            using System;
            public class Payload { public object Run() => {{expression}}; }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Wrong.Context", "Old"), new("Destination.Context", "Old.Context")]),
        });
        Assert.Contains(expected, result);
        Assert.Empty(issues);
        Assert.DoesNotContain("using System;", result);
    }

    [Theory]
    [InlineData("public static class Old { public static object Context => null; }")]
    [InlineData("public class Holder { public object Context; } public static class OldHolder { }")]
    public async Task PreservesResolvedReceivers(string definitions)
    {
        string source = definitions.StartsWith("public static class Old", StringComparison.Ordinal)
            ? definitions + " public class Payload { public object Run() => Old.Context.ToString(); }"
            : definitions + " public class Payload { public Holder Old; public object Run() => Old.Context.ToString(); }";
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Context", "Old.Context")]),
        });
        Assert.DoesNotContain("Destination.Context", result);
        Assert.Empty(issues);
    }

    [Fact]
    public async Task ReportsOverlapForQualifiedReceiver()
    {
        const string source = "public class Payload { public object Run() => Old.Context.Create(); }";
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Context", "Old.Context")], [new("CS0103", "Old")]),
        });
        Assert.Contains("global::Destination.Context.Create()", result);
        MappingIgnoreOverlap warning = Assert.IsType<MappingIgnoreOverlap>(Assert.Single(issues));
        Assert.Equal("Old.Context", warning.Reference);
        Assert.Equal("global::Destination.Context", warning.Destination);
        Assert.Equal("CS0103", warning.DiagnosticID);
    }

    [Fact]
    public async Task OutputCompilesWithDestinationDefinitions()
    {
        const string source = "public class Payload { public object Run() => Old.Context<int>.Create<string>(); }";
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        var rewriter = new CSharpRewriter
        {
            AddEmbeddedAttribute = false,
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Context", "Old.Context")]),
        };

        string result = rewriter.Rewrite(source, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
        Assert.Empty(issues);
        SyntaxTree original = Assert.Single(compilation.SyntaxTrees, tree => tree.GetText(TestContext.Current.CancellationToken).ToString() == source);
        var options = (CSharpParseOptions)original.Options;
        SyntaxTree rewritten = CSharpSyntaxTree.ParseText(result, options, cancellationToken: TestContext.Current.CancellationToken);
        SyntaxTree destination = CSharpSyntaxTree.ParseText(
            "namespace Destination { public class Context<T> { public static object Create<U>() => null; } }",
            options,
            cancellationToken: TestContext.Current.CancellationToken);
        Compilation output = compilation.ReplaceSyntaxTree(original, rewritten).AddSyntaxTrees(destination);
        Assert.Empty(output.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    private static async Task<(string Result, RewriteIssue[] Issues)> Rewrite(string source, CSharpRewriter rewriter)
    {
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        string result = rewriter.Rewrite(source, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
        return (result, [.. issues]);
    }
}
