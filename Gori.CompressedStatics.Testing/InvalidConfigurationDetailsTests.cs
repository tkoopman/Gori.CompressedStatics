using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public sealed class InvalidConfigurationDetailsTests
{
    public static TheoryData<string, string, string?, int?, string> Cases
        => [
            new("CSharpRewriterTypeMapping(\"Bad[]\")", "fullTypeName", "Bad[]", null, "Type mappings require") { Label = "Invalid destination" },
            new("CSharpRewriterTypeMapping(\"Library.Type\", \"Bad[]\")", "replaces", "Bad[]", null, "Type mappings require") { Label = "Invalid replacement" },
            new("CSharpRewriterTypeMapping(null)", "fullTypeName", null, null, "non-null") { Label = "Null destination" },
            new("CSharpRewriterIgnore(\" \" )", "diagnosticID", " ", null, "diagnostic ID") { Label = "Blank diagnostic ID" },
            new("CSharpRewriterTypeMapping(\"Library.Extensions\", ExtensionMethods = null)", "ExtensionMethods", null, null, "non-null array") { Label = "Null extension array" },
            new("CSharpRewriterTypeMapping(\"Library.Extensions\", ExtensionMethods = [\"Valid\", \"Bad(\"])", "ExtensionMethods", "Bad(", 1, "format") { Label = "Malformed second extension" },
            new("CSharpRewriterTypeMapping(\"Library.Extensions\", ExtensionMethods = [null])", "ExtensionMethods", null, 0, "non-null string") { Label = "Null extension element" },
        ];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task CapturesFailureMessageAndArgumentContext(string attribute, string argument, string? value, int? index, string message)
    {
        string source = "using Gori.CompressedStatics; [assembly: " + attribute + "] public class Payload { }";
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        _ = CSharpRewriterConfigurationReader.Load(compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
        InvalidConfiguration issue = Assert.IsType<InvalidConfiguration>(Assert.Single(issues));
        Assert.Equal(InvalidConfigurationReason.InvalidValue, issue.Reason);
        Assert.Equal(argument, issue.ArgumentName);
        Assert.Equal(value, issue.ArgumentValue);
        Assert.Equal(index, issue.ArgumentIndex);
        Assert.Contains(message, issue.Message);
        Assert.Equal(attribute, issue.Value);
        Assert.NotNull(issue.Location);
        Assert.Equal(attribute, issue.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(issue.Location.SourceSpan));
    }
}
