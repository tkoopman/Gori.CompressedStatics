using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public sealed class ContainingTypeArgumentMappingTests
{
    public static TheoryData<string, string> References
        => [
            new("public Outer<int>.Inner<string> Value;", "Outer<int>.Inner<string>") { Label = "Type declaration" },
            new("public object Run() => Outer<int>.Inner<string>.Create();", "Outer<int>.Inner<string>") { Label = "Static call" },
            new("public object Run() => Outer<int>.Inner<string>.Current;", "Outer<int>.Inner<string>") { Label = "Static property" },
        ];

    [Theory]
    [MemberData(nameof(References))]
    public async Task RejectsContainingArgumentsWithoutFallingBackToShorterMapping(string member, string reference)
    {
        string source = "using System; public class Payload { " + member + " }";
        (string result, RewriteIssue[] issues) = await Rewrite(source, [
            new("Destination.Container", "Outer.Inner"),
            new("Fallback.Container", "Outer"),
        ]);

        Assert.Contains(member, result);
        Assert.Contains("using System;", result);
        Assert.DoesNotContain("Destination.Container", result);
        Assert.DoesNotContain("Fallback.Container", result);
        InvalidConfiguration issue = Assert.Single(issues.OfType<InvalidConfiguration>());
        Assert.Equal(InvalidConfigurationReason.UnsupportedContainingTypeArguments, issue.Reason);
        Assert.Equal(reference, issue.Value);
        Assert.NotNull(issue.Location);
        Assert.Equal(reference, issue.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(issue.Location.SourceSpan));
        Assert.Contains(issues, item => item is Unresolvable or BindingIssue);
    }

    [Theory]
    [MemberData(nameof(References))]
    public async Task MappingOuterTypePreservesBothArgumentLists(string member, string reference)
    {
        string source = "using System; public class Payload { " + member + " }";
        (string result, RewriteIssue[] issues) = await Rewrite(source, [new("Destination.Container", "Outer")]);

        Assert.Empty(issues);
        Assert.Contains(member.Replace(reference, "global::Destination.Container<int>.Inner<string>", StringComparison.Ordinal), result);
        Assert.DoesNotContain("using System;", result);
    }

    [Fact]
    public async Task AllowsGenericArgumentsOnLastMatchedSegment()
    {
        const string source = "public class Payload { public Outer.Inner<string> Value; }";
        (string result, RewriteIssue[] issues) = await Rewrite(source, [new("Destination.Container", "Outer.Inner")]);

        Assert.Empty(issues);
        Assert.Contains("global::Destination.Container<string> Value", result);
    }

    private static async Task<(string Result, RewriteIssue[] Issues)> Rewrite(string source, TypeMapping[] mappings)
    {
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        var rewriter = new CSharpRewriter { RewriteTypeNames = true, Configuration = RewriterTestConfiguration.Create([.. mappings]) };
        string result = rewriter.Rewrite(source, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
        return (result, [.. issues]);
    }
}
