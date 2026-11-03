using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using CommonMockProject = Gori.Common.Roslyn.Test.MockProject;

namespace Gori.CompressedStatics.Testing;

public sealed class RewriterConfigurationIntegrationTests
{
    [Fact]
    public async Task HostAssemblyConfigurationIsReusedAcrossExternalPayloads()
    {
        const string host = """
            using Gori.CompressedStatics;
            [assembly: CSharpRewriterTypeMapping("Assembly.Context")]
            [assembly: CSharpRewriterIgnore("CS0246", "GlobalMissing")]
            public class Host { }
            """;
        var project = CommonMockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, host, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        int originalTreeCount = compilation.SyntaxTrees.Count();
        RewriterConfiguration configuration = CSharpRewriterConfigurationReader.Load(
            compilation, out IEnumerable<RewriteIssue> loadingIssues, TestContext.Current.CancellationToken);
        Assert.Empty(loadingIssues);
        var rewriter = new CSharpRewriter { RewriteTypeNames = true, Configuration = configuration };

        foreach (string name in new[] { "First", "Second" })
        {
            string source = "public class " + name + " { public Context Value; public GlobalMissing Missing; }";
            string result = rewriter.Rewrite(source, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
            Assert.Empty(issues);
            Assert.Contains("global::Assembly.Context Value", result);
            Assert.Contains("GlobalMissing Missing", result);
            Assert.Same(configuration, rewriter.Configuration);
        }

        Assert.Equal(originalTreeCount, compilation.SyntaxTrees.Count());
        Assert.Equal("Assembly.Context", configuration.FindTypeMapping(["Context"])?.FullTypeName);
    }

    [Fact]
    public async Task ExternalAttributesAreNeitherLoadedNorStripped()
    {
        const string host = "public class Host { }";
        const string payload = """
            using Gori.CompressedStatics;
            [assembly: CSharpRewriterTypeMapping("File.Context")]
            [assembly: CSharpRewriterIgnore("CS0246", "FileMissing")]
            public class Payload { public Context Value; public FileMissing Missing; }
            """;
        var project = CommonMockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, host, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        RewriterConfiguration configuration = CSharpRewriterConfigurationReader.Load(
            compilation, out IEnumerable<RewriteIssue> loadingIssues, TestContext.Current.CancellationToken);
        Assert.Empty(loadingIssues);
        Assert.Same(RewriterConfiguration.Empty, configuration);
        var rewriter = new CSharpRewriter { RewriteTypeNames = true, Configuration = configuration };

        string result = rewriter.Rewrite(payload, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);

        Assert.Equal(2, issues.Count());
        Assert.All(issues, issue => Assert.IsType<Unresolvable>(issue));
        Assert.Contains(issues.OfType<Unresolvable>(), issue => issue.Name == "Context");
        Assert.Contains(issues.OfType<Unresolvable>(), issue => issue.Name == "FileMissing");
        Assert.Contains("global::Gori.CompressedStatics.CSharpRewriterTypeMappingAttribute", result);
        Assert.Contains("global::Gori.CompressedStatics.CSharpRewriterIgnoreAttribute", result);
        Assert.Contains("using Gori.CompressedStatics;", result);
        Assert.Contains("Context Value", result);
        Assert.DoesNotContain("global::File.Context", result);
    }

    [Fact]
    public async Task DoesNotAutomaticallyLoadAssemblyConfigurationOrRepeatLoadingIssues()
    {
        const string source = """
            using Gori.CompressedStatics;
            [assembly: CSharpRewriterTypeMapping("Assembly.Context")]
            [assembly: CSharpRewriterTypeMapping("Bad[]")]
            public class Payload { public Context Value; }
            """;
        var project = CommonMockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        string result = new CSharpRewriter { RewriteTypeNames = true }.Rewrite(
            source, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("global::Assembly.Context", result);
        Assert.Contains("global::Gori.CompressedStatics.CSharpRewriterTypeMappingAttribute", result);
        _ = Assert.IsType<Unresolvable>(Assert.Single(issues));

        RewriterConfiguration configuration = CSharpRewriterConfigurationReader.Load(
            compilation, out IEnumerable<RewriteIssue> loadingIssues, TestContext.Current.CancellationToken);
        Assert.Equal(InvalidConfigurationReason.InvalidValue, Assert.IsType<InvalidConfiguration>(Assert.Single(loadingIssues)).Reason);
        result = new CSharpRewriter { RewriteTypeNames = true, Configuration = configuration }.Rewrite(
            source, compilation, out issues, TestContext.Current.CancellationToken);
        Assert.Empty(issues);
        Assert.Contains("global::Assembly.Context Value", result);
    }

    [Fact]
    public void DoesNotAutomaticallyIgnoreMissingPredefinedTypes()
    {
        const string source = "public class Payload { public object Value; }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = CSharpCompilation.Create("MissingRuntime", [tree],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Contains(compilation.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Id == "CS0518");
        RewriterConfiguration configuration = CSharpRewriterConfigurationReader.Load(
            compilation, out IEnumerable<RewriteIssue> loadingIssues, TestContext.Current.CancellationToken);
        Assert.Empty(loadingIssues);
        Assert.Empty(configuration.Ignores);
        Assert.Empty(configuration.GetMatchingIgnores("CS0518", "Predefined type 'System.Object' is not defined or imported"));
        Assert.Empty(configuration.GetMatchingIgnores("CS0656", "Missing compiler required member"));

        string result = new CSharpRewriter { Configuration = configuration }.Rewrite(
            source, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);

        Assert.Contains("object Value", result);
        Assert.Contains(issues.OfType<Unresolvable>(), issue => issue.Name == "object");
    }

    [Fact]
    public void EquivalentConfigurationsPreserveRewriterValueEquality()
    {
        RewriterConfiguration first = RewriterTestConfiguration.Create([new("Assembly.First"), new("Assembly.Second")]);
        RewriterConfiguration second = RewriterTestConfiguration.Create([new("Assembly.Second"), new("Assembly.First")]);
        var firstRewriter = new CSharpRewriter { Configuration = first };
        var secondRewriter = new CSharpRewriter { Configuration = second };
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(firstRewriter, secondRewriter);
        Assert.Equal(firstRewriter.GetHashCode(), secondRewriter.GetHashCode());
        Assert.NotEqual(firstRewriter, new CSharpRewriter { Configuration = RewriterTestConfiguration.Create([new("Other.First")]) });
        Assert.Equal(new CSharpRewriter(), new CSharpRewriter { Configuration = RewriterConfiguration.Empty });
        Assert.Equal(new CSharpRewriter().GetHashCode(), new CSharpRewriter { Configuration = RewriterConfiguration.Empty }.GetHashCode());
    }
}
