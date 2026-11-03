using System.Collections.Immutable;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gori.CompressedStatics.Testing;

public sealed class CSharpRewriterGlobalUsingsTests
{
    private const string GlobalUsingsSource = """
        global using System.Linq;
        global using System.Text;
        global using Builder = System.Text.StringBuilder;
        global using static System.Math;
        public static class Host { }
        """;

    [Fact]
    public async Task AddsOnlyUsedGlobalNamespaceUsingsWhenImportCleanupIsDisabled()
    {
        const string payload = "public class Payload { public int Read() { var query = from value in new[] { 4 } select value; return query.First(); } }";
        Compilation compilation = await CreateCompilation();
        var transformer = new CSharpRewriterTransformer(CSharpRewriterOptions.AddGlobalUsings);

        Assert.True(transformer.TryTransform(
            CreateTransformerInput(payload),
            compilation,
            out string? rewritten,
            out ImmutableArray<DiagnosticInfo> diagnostics,
            TestContext.Current.CancellationToken));

        Assert.True(diagnostics.IsDefaultOrEmpty);
        Assert.Contains("using System.Linq;", rewritten, StringComparison.Ordinal);
        Assert.DoesNotContain("using System.Text;", rewritten, StringComparison.Ordinal);
        Assert.DoesNotContain("global using", rewritten, StringComparison.Ordinal);
        AssertCompilesWithoutProducerGlobalUsings(rewritten!, compilation);
    }

    [Fact]
    public async Task DefaultRewriteDoesNotKeepGlobalUsingMadeUnnecessaryByExtensionRewriting()
    {
        const string payload = "public class Payload { public int Read() => new[] { 4 }.First(); }";
        Compilation compilation = await CreateCompilation();
        var transformer = new CSharpRewriterTransformer { AddEmbeddedAttribute = false };

        Assert.True(transformer.TryTransform(
            CreateTransformerInput(payload),
            compilation,
            out string? rewritten,
            out ImmutableArray<DiagnosticInfo> diagnostics,
            TestContext.Current.CancellationToken));

        Assert.True(diagnostics.IsDefaultOrEmpty);
        Assert.DoesNotContain("using System.Linq;", rewritten, StringComparison.Ordinal);
        Assert.Contains("global::System.Linq.Enumerable.First<int>", rewritten, StringComparison.Ordinal);
        AssertCompilesWithoutProducerGlobalUsings(rewritten!, compilation);
    }

    [Fact]
    public async Task AddsOnlyUsedGlobalAliasAndStaticUsings()
    {
        const string payload = "public class Payload { public Builder Create() => new(); public int Read() => Abs(-1); }";
        Compilation compilation = await CreateCompilation();
        var transformer = new CSharpRewriterTransformer(CSharpRewriterOptions.AddGlobalUsings);

        Assert.True(transformer.TryTransform(
            CreateTransformerInput(payload),
            compilation,
            out string? rewritten,
            out ImmutableArray<DiagnosticInfo> diagnostics,
            TestContext.Current.CancellationToken));

        Assert.True(diagnostics.IsDefaultOrEmpty, string.Join(", ", diagnostics.Select(diagnostic => diagnostic.Descriptor.Id)));
        Assert.Contains("using Builder = System.Text.StringBuilder;", rewritten, StringComparison.Ordinal);
        Assert.Contains("using static System.Math;", rewritten, StringComparison.Ordinal);
        Assert.DoesNotContain("using System.Linq;", rewritten, StringComparison.Ordinal);
        AssertCompilesWithoutProducerGlobalUsings(rewritten!, compilation);
    }

    [Fact]
    public async Task DoesNotAddCompilationGlobalUsingsWhenOptionIsDisabled()
    {
        const string payload = "public class Payload { }";
        Compilation compilation = await CreateCompilation();
        var transformer = new CSharpRewriterTransformer(CSharpRewriterOptions.None);

        Assert.True(transformer.TryTransform(
            CreateTransformerInput(payload),
            compilation,
            out string? rewritten,
            out ImmutableArray<DiagnosticInfo> diagnostics,
            TestContext.Current.CancellationToken));

        Assert.True(diagnostics.IsDefaultOrEmpty);
        Assert.DoesNotContain("using System.Linq;", rewritten, StringComparison.Ordinal);
        Assert.DoesNotContain("using System.Text;", rewritten, StringComparison.Ordinal);
    }

    private static async Task<Compilation> CreateCompilation()
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(GlobalUsingsSource, "src\\GlobalUsings.cs");
        return await project.GetCompilationAsync();
    }

    private static CompressAttributeInfo CreateTransformerInput(string payload)
        => new()
        {
            ContainingType = default,
            MemberName = "Test",
            MemberKind = MemberKind.Property,
            Accessibility = "public",
            IsPartial = true,
            ReturnType = ReturnType.String,
            AttributeType = CompressAttributeType.CompressFile,
            Location = default,
            SourceValue = payload,
        };

    private static void AssertCompilesWithoutProducerGlobalUsings(string rewritten, Compilation producerCompilation)
    {
        var parseOptions = (CSharpParseOptions)producerCompilation.SyntaxTrees.First().Options;
        SyntaxTree rewrittenTree = CSharpSyntaxTree.ParseText(rewritten, parseOptions, cancellationToken: TestContext.Current.CancellationToken);
        Compilation outputCompilation = CSharpCompilation.Create(
            "RewrittenPayload",
            [rewrittenTree],
            producerCompilation.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.Empty(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }
}
