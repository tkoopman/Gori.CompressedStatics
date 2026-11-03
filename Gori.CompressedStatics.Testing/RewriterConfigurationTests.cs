using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;

using CommonMockProject = Gori.Common.Roslyn.Test.MockProject;

namespace Gori.CompressedStatics.Testing;

public sealed class RewriterConfigurationTests
{
    [Fact]
    public async Task LoadsAssemblyRulesWithOptionalArgumentsAndExtensions()
    {
        Compilation compilation = await CreateCompilation("""
            using Gori.CompressedStatics;
            [assembly: CSharpRewriterIgnore("CS0246")]
            [assembly: CSharpRewriterTypeMapping("Destination.Context", ExtensionMethods = new[] { "Read(*)" })]
            """);

        RewriterConfiguration configuration = CSharpRewriterConfigurationReader.Load(
            compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);

        Assert.Empty(issues);
        Ignore ignore = Assert.Single(configuration.GetMatchingIgnores("CS0246", "Missing type"));
        Assert.Null(ignore.MessageContains);
        TypeMapping mapping = Assert.Single(configuration.TypeMappings.Values);
        Assert.Equal("Context", mapping.Replaces);
        Assert.Equal("Destination.Context", mapping.FullTypeName);
        _ = Assert.Single(mapping.ExtensionMethods);
    }

    [Fact]
    public void EmptyInputReusesEmptyConfiguration()
    {
        Assert.Same(RewriterConfiguration.Empty, RewriterConfiguration.Create(
            [], [], out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken));
        Assert.Empty(issues);
    }

    [Fact]
    public void LongestReplacementWinsWithoutMatchingPartialNames()
    {
        RewriterConfiguration configuration = RewriterTestConfiguration.Create([
            new("Destination.Context", "Context"),
            new("Destination.Nested", "Context.Nested"),
        ]);

        Assert.Equal("Destination.Context", configuration.FindTypeMapping(["Context"])?.FullTypeName);
        Assert.Equal("Destination.Nested", configuration.FindTypeMapping(["Context", "Nested", "More"])?.FullTypeName);
        Assert.Null(configuration.FindTypeMapping(["Contexts"]));
    }

    public static TheoryData<bool> MappingOrders
        => [
            new(false) { Label = "Exact attribute name first" },
            new(true) { Label = "Attribute suffix first" },
        ];

    [Theory]
    [MemberData(nameof(MappingOrders))]
    public void AttributeSuffixTiesPreferExactNameRegardlessOfInsertionOrder(bool reverse)
    {
        TypeMapping[] mappings = [
            new("Destination.Exact", "Marker"),
            new("Destination.Suffixed", "MarkerAttribute"),
            new("Destination.Nested", "Marker.Nested"),
        ];
        RewriterConfiguration configuration = RewriterTestConfiguration.Create(
            [.. reverse ? mappings.Reverse() : mappings]);

        Assert.Equal("Destination.Exact", configuration.FindTypeMapping(["Marker"], attribute: true)?.FullTypeName);
        Assert.Equal("Destination.Suffixed", configuration.FindTypeMapping(["MarkerAttribute"], attribute: true)?.FullTypeName);
        Assert.Equal("Destination.Nested", configuration.FindTypeMapping(["Marker", "Nested"], attribute: true)?.FullTypeName);
    }

    [Fact]
    public void ConflictsDiscardOnlyTheConflictingMapping()
    {
        var configuration = RewriterConfiguration.Create([], [
            new("First.Context", "Context"),
            new("Second.Context", "Context"),
            new("Destination.Valid"),
        ], out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);

        InvalidConfiguration issue = Assert.IsType<InvalidConfiguration>(Assert.Single(issues));
        Assert.Equal(InvalidConfigurationReason.ConflictingRules, issue.Reason);
        Assert.Null(configuration.FindTypeMapping(["Context"]));
        Assert.Equal("Destination.Valid", Assert.Single(configuration.TypeMappings.Values).FullTypeName);
    }

    [Fact]
    public async Task InvalidRulesReportDetailsWithoutDiscardingValidRules()
    {
        Compilation compilation = await CreateCompilation("""
            using Gori.CompressedStatics;
            [assembly: CSharpRewriterIgnore("NotACompilerId")]
            [assembly: CSharpRewriterTypeMapping("Bad[]")]
            [assembly: CSharpRewriterTypeMapping("Destination.Valid")]
            """);

        RewriterConfiguration configuration = CSharpRewriterConfigurationReader.Load(
            compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
        InvalidConfiguration[] failures = [.. issues.Cast<InvalidConfiguration>()];

        Assert.Equal(2, failures.Length);
        Assert.All(failures, issue =>
        {
            Assert.Equal(InvalidConfigurationReason.InvalidValue, issue.Reason);
            Assert.NotNull(issue.Location);
            Assert.False(string.IsNullOrWhiteSpace(issue.Message));
        });
        Assert.Equal("diagnosticID", failures[0].ArgumentName);
        Assert.Equal("NotACompilerId", failures[0].ArgumentValue);
        Assert.Equal("fullTypeName", failures[1].ArgumentName);
        Assert.Equal("Bad[]", failures[1].ArgumentValue);
        Assert.Equal("Destination.Valid", Assert.Single(configuration.TypeMappings.Values).FullTypeName);
    }

    [Fact]
    public void IgnoresUseOrdinalFiltersAndDeduplication()
    {
        RewriterConfiguration configuration = RewriterTestConfiguration.Create([], [
            new("CS0246", "Missing"), new("CS0246", "Missing"), new("CS1061"),
        ]);

        Assert.Equal(2, configuration.Ignores.Length);
        _ = Assert.Single(configuration.GetMatchingIgnores("CS0246", "Missing type"));
        Assert.Empty(configuration.GetMatchingIgnores("CS0246", "missing type"));
        Assert.Empty(configuration.GetMatchingIgnores("CS9999", "Missing type"));
        _ = Assert.Single(configuration.GetMatchingIgnores("CS1061", "Any message"));
    }

    [Fact]
    public async Task InvalidExtensionRetainsAttributeAndElementContext()
    {
        Compilation compilation = await CreateCompilation("""
            using Gori.CompressedStatics;
            [assembly: CSharpRewriterTypeMapping("Destination.Context", ExtensionMethods = new[] { "Read(*)", "Read(var)" })]
            [assembly: CSharpRewriterTypeMapping("Destination.Valid")]
            """);

        RewriterConfiguration configuration = CSharpRewriterConfigurationReader.Load(
            compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);

        InvalidConfiguration issue = Assert.IsType<InvalidConfiguration>(Assert.Single(issues));
        Assert.Equal("ExtensionMethods", issue.ArgumentName);
        Assert.Equal(1, issue.ArgumentIndex);
        Assert.Equal("Read(var)", issue.ArgumentValue);
        Assert.Equal("Type name cannot be 'var'.", issue.Message);
        Assert.NotNull(issue.Location);
        Assert.Equal("Destination.Valid", Assert.Single(configuration.TypeMappings.Values).FullTypeName);
    }

    [Fact]
    public void EmptyInputStillHonorsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _ = Assert.Throws<OperationCanceledException>(() => RewriterConfiguration.Create(
            [], [], out _, cancellation.Token));
    }

    private static async Task<Compilation> CreateCompilation(string source)
    {
        var project = CommonMockProject.Create(
            typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        return await project.GetCompilationAsync();
    }
}
