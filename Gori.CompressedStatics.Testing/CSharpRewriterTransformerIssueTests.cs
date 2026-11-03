using System.Collections.Immutable;

using Gori.CompressedStatics.CSharp;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using CommonMockProject = Gori.Common.Roslyn.Test.MockProject;

namespace Gori.CompressedStatics.Testing;

public sealed class CSharpRewriterTransformerIssueTests
{
    [Fact]
    public async Task ReportsBindingIssueWithItsCompilerIdAndMessage()
    {
        const string source = "public class Sample { public int Value = \"text\"; }";

        DiagnosticInfo diagnostic = Assert.Single(await Transform(source, CSharpRewriterOptions.None));

        Assert.Equal("GCS038", diagnostic.Descriptor.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("CS0029", diagnostic.Properties["CompilerDiagnosticId"]);
        Assert.Contains("Cannot implicitly convert type", diagnostic.Properties["CompilerDiagnosticMessage"], StringComparison.Ordinal);
        Assert.Contains("CS0029", diagnostic.ToDiagnostic().GetMessage(), StringComparison.Ordinal);
        Assert.Contains("Cannot implicitly convert type", diagnostic.ToDiagnostic().GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsInvalidConfigurationDetails()
    {
        const string source = "using Gori.CompressedStatics; [assembly: CSharpRewriterTypeMapping(\"Bad[]\")] public class Sample { }";

        DiagnosticInfo diagnostic = Assert.Single(await Transform(source, CSharpRewriterOptions.None));

        Assert.Equal(DiagnosticDescriptors.CSharpRewriterInvalidConfiguration.Id, diagnostic.Descriptor.Id);
        Assert.Contains("Type mappings require", diagnostic.ToDiagnostic().GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsUnresolvableType()
    {
        const string source = "public class Sample { public MissingType Value; }";

        DiagnosticInfo diagnostic = Assert.Single(await Transform(source, CSharpRewriterOptions.RewriteTypeNames));

        Assert.Equal("GCS039", diagnostic.Descriptor.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("UnresolvableType", diagnostic.Properties["UnresolvableType"]);
        Assert.Equal("MissingType", diagnostic.Properties["UnresolvableName"]);
        Assert.Contains("unresolvable type", diagnostic.ToDiagnostic().GetMessage(), StringComparison.Ordinal);
        Assert.Contains("MissingType", diagnostic.ToDiagnostic().GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task IgnoresPlaceholderTypeMappingsDuringTransformation()
    {
        const string source = "public class Sample { public MissingType Value; }";
        const string assemblySource = "using Gori.CompressedStatics; [assembly: CSharpRewriterTypeMapping(\"__CSharpRewriterPlaceholder__.Context\", \"MissingType\")]";

        (string? output, ImmutableArray<DiagnosticInfo> diagnostics) = await TransformOutput(
            source, CSharpRewriterOptions.RewriteTypeNames, assemblySource);

        DiagnosticInfo diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.CSharpRewriterUnresolvable.Id, diagnostic.Descriptor.Id);
        Assert.Contains("MissingType", output, StringComparison.Ordinal);
        Assert.DoesNotContain(Const.PlaceholderNamespace, output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IncludesIssueSourceLocationInDiagnosticMessage()
    {
        const string source = """
            public class Sample
            {
                public MissingType Value;
            }
            """;

        DiagnosticInfo diagnostic = Assert.Single(await Transform(source, CSharpRewriterOptions.RewriteTypeNames));
        string message = diagnostic.ToDiagnostic().GetMessage();

        Assert.Contains("MissingType' (source location: ", message, StringComparison.Ordinal);
        Assert.Contains("(3,12)", message, StringComparison.Ordinal);
        Assert.Null(diagnostic.Location);
    }

    [Fact]
    public async Task ReportsMappingIgnoreOverlapAsWarning()
    {
        const string source = "using Gori.CompressedStatics; [assembly: CSharpRewriterIgnore(\"CS0246\")] [assembly: CSharpRewriterTypeMapping(\"Destination.Context\", \"MissingType\")] public class Sample { public MissingType Value; }";

        DiagnosticInfo diagnostic = Assert.Single(await Transform(source, CSharpRewriterOptions.RewriteTypeNames));

        Assert.Equal(DiagnosticDescriptors.CSharpRewriterMappingIgnoreOverlap.Id, diagnostic.Descriptor.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("CS0246", diagnostic.ToDiagnostic().GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsAdditionalFileIssueAtCompressionAttributeAndStillGeneratesSource()
    {
        const string source = "using Gori.CompressedStatics; public static partial class Sample { [CompressFile(\"payload.cs\", Transformer = \"CSharpRewriter\")] public static partial string Value { get; } }";
        const string payload = "public class Payload { public MissingType Value; }";
        var project = new CommonMockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(source, "src\\Sample.cs");
        _ = project.AddAdditionalFile("src\\payload.cs", payload);
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(await project.GetCompilationAsync(), TestContext.Current.CancellationToken);

        GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);
        Assert.Null(result.Exception);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item => item.Id == DiagnosticDescriptors.CSharpRewriterUnresolvable.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(source.IndexOf("CompressFile(", StringComparison.Ordinal), diagnostic.Location.SourceSpan.Start);
        _ = Assert.Single(result.GeneratedSources);
    }

    [Fact]
    public async Task AssemblyRulesApplyToAdditionalFileAndReportLoadingIssues()
    {
        const string source = """
            using Gori.CompressedStatics;
            [assembly: CSharpRewriterIgnore("CS0246", "IgnoredMissing")]
            [assembly: CSharpRewriterTypeMapping("Destination.Context", "MappedMissing")]
            [assembly: CSharpRewriterTypeMapping("Bad[]")]
            [assembly: CSharpRewriterTypeMapping("First.Conflict")]
            [assembly: CSharpRewriterTypeMapping("Second.Conflict")]
            public static partial class Sample
            {
                [CompressFile("payload.cs", Transformer = "CSharpRewriter")]
                public static partial string Value { get; }
            }
            """;
        const string payload = "public class Payload { public MappedMissing First; public IgnoredMissing Second; }";
        var project = new CommonMockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(source, "src\\Sample.cs");
        _ = project.AddAdditionalFile("src\\payload.cs", payload);
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(await project.GetCompilationAsync(), TestContext.Current.CancellationToken);

        GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);
        Assert.Null(result.Exception);
        Assert.Equal(2, result.Diagnostics.Length);
        Diagnostic invalid = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == DiagnosticDescriptors.CSharpRewriterInvalidConfiguration.Id);
        Diagnostic conflict = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == DiagnosticDescriptors.CSharpRewriterConflictingConfiguration.Id);
        Assert.Equal(source.IndexOf("CompressFile(", StringComparison.Ordinal), invalid.Location.SourceSpan.Start);
        Assert.Equal(invalid.Location, conflict.Location);
        Assert.Contains("source location:", invalid.GetMessage(), StringComparison.Ordinal);
        _ = Assert.Single(result.GeneratedSources);
    }

    [Fact]
    public async Task RetainsAssemblyIssuesWhenPayloadHasInvalidSyntax()
    {
        ImmutableArray<DiagnosticInfo> diagnostics = await Transform("public class Broken {",
            CSharpRewriterOptions.None,
            "using Gori.CompressedStatics; [assembly: CSharpRewriterTypeMapping(\"Bad[]\")]",
            expectSuccess: false);

        Assert.Equal(2, diagnostics.Length);
        _ = Assert.Single(diagnostics, diagnostic => diagnostic.Descriptor.Id == DiagnosticDescriptors.CSharpRewriterInvalidConfiguration.Id);
        _ = Assert.Single(diagnostics, diagnostic => diagnostic.Descriptor.Id == DiagnosticDescriptors.InvalidCSharpTransformerSource.Id);
    }

    public static TheoryData<string, string> WarningIssues
        => [
            new("public int Value = \"text\";", "GCS038") { Label = "Binding warning" },
            new("public Missing Value;", "GCS039") { Label = "Unresolvable warning" },
        ];

    [Theory]
    [MemberData(nameof(WarningIssues))]
    public async Task WarningIssuesPreserveOutputAndConservativeImports(string member, string diagnosticID)
    {
        string source = "using System; public class Payload { " + member + " }";
        (string? result, ImmutableArray<DiagnosticInfo> diagnostics) = await TransformOutput(source, CSharpRewriterOptions.RewriteTypeNames);

        DiagnosticInfo diagnostic = Assert.Single(diagnostics);
        Assert.Equal(diagnosticID, diagnostic.Descriptor.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.NotNull(result);
        Assert.Contains("using System;", result);
        Assert.Contains(member, result);
    }

    [Fact]
    public void MissingRequiredMemberCompilerSupportIsAWarningWithoutAutomaticSuppression()
    {
        const string support = """
            namespace System
            {
                public class Object { }
                public abstract class ValueType { }
                public struct Void { }
                public struct Boolean { }
                public struct Int32 { }
                public sealed class String { }
                public abstract class Attribute { }
                public sealed class ObsoleteAttribute : Attribute
                {
                    public ObsoleteAttribute(string message, bool error) { }
                }
            }
            namespace System.Runtime.CompilerServices
            {
                public sealed class RequiredMemberAttribute : System.Attribute { }
                public sealed class IsExternalInit { }
            }
            """;
        const string source = """
            using System;
            public class Payload
            {
                public required object Value { get; init; }
                public int Broken = "text";
            }
            """;
        Compilation compilation = CSharpCompilation.Create("LegacyRuntime", [CSharpSyntaxTree.ParseText(support, cancellationToken: TestContext.Current.CancellationToken)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Null(compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute"));
        Assert.NotNull(compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.RequiredMemberAttribute"));
        RewriterConfiguration configuration = CSharpRewriterConfigurationReader.Load(
            compilation, out IEnumerable<RewriteIssue> loadingIssues, TestContext.Current.CancellationToken);
        Assert.Empty(loadingIssues);
        Assert.Empty(configuration.Ignores);
        var input = new CompressAttributeInfo
        {
            ContainingType = default,
            MemberName = "Value",
            MemberKind = MemberKind.Property,
            Accessibility = "public",
            IsPartial = true,
            ReturnType = ReturnType.String,
            AttributeType = CompressAttributeType.CompressFile,
            Location = default,
            SourceValue = source,
        };

        Assert.True(new CSharpRewriterTransformer(CSharpRewriterOptions.RewriteTypeNames).TryTransform(
            input, compilation, out string? result, out ImmutableArray<DiagnosticInfo> diagnostics,
            TestContext.Current.CancellationToken));

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal("GCS038", diagnostic.Descriptor.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        });
        Assert.Contains(diagnostics, diagnostic => diagnostic.ToDiagnostic().GetMessage().Contains("CS0656", StringComparison.Ordinal)
            && diagnostic.ToDiagnostic().GetMessage().Contains("CompilerFeatureRequiredAttribute", StringComparison.Ordinal));
        Assert.Contains(diagnostics, diagnostic => diagnostic.ToDiagnostic().GetMessage().Contains("CS0029", StringComparison.Ordinal));
        Assert.NotNull(result);
        Assert.Contains("using System;", result);
        Assert.Contains("required object Value", result);
        Assert.Contains("public int Broken = \"text\";", result);
    }

    [Fact]
    public async Task HostAssemblyRulesApplyToMultiplePayloadsWithoutLoadingPayloadAttributes()
    {
        const string host = "using Gori.CompressedStatics; [assembly: CSharpRewriterTypeMapping(\"Destination.Context\")]";
        var project = CommonMockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, host, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        var transformer = new CSharpRewriterTransformer(CSharpRewriterOptions.RewriteTypeNames);
        foreach (string name in new[] { "First", "Second" })
        {
            string source = "using Gori.CompressedStatics; [assembly: CSharpRewriterTypeMapping(\"Payload.Context\")] public class " + name + " { public Context Value; }";
            var input = new CompressAttributeInfo
            {
                ContainingType = default,
                MemberName = "Value",
                MemberKind = MemberKind.Property,
                Accessibility = "public",
                IsPartial = true,
                ReturnType = ReturnType.String,
                AttributeType = CompressAttributeType.CompressFile,
                Location = default,
                SourceValue = source,
            };

            Assert.True(transformer.TryTransform(input, compilation, out string? result,
                out ImmutableArray<DiagnosticInfo> diagnostics, TestContext.Current.CancellationToken));
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            Assert.Contains("global::Destination.Context Value", result);
            Assert.Contains("global::Gori.CompressedStatics.CSharpRewriterTypeMappingAttribute", result);
            Assert.Contains("Payload.Context", result);
        }
    }

    private static async Task<ImmutableArray<DiagnosticInfo>> Transform(
        string source,
        CSharpRewriterOptions options,
        string? assemblySource = null,
        bool expectSuccess = true)
        => (await TransformOutput(source, options, assemblySource, expectSuccess)).Diagnostics;

    private static async Task<(string? Output, ImmutableArray<DiagnosticInfo> Diagnostics)> TransformOutput(
        string source,
        CSharpRewriterOptions options,
        string? assemblySource = null,
        bool expectSuccess = true)
    {
        var project = CommonMockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        if (assemblySource is not null)
        {
            _ = project.AddDocument(assemblySource, "src\\AssemblyConfiguration.cs");
        }

        Compilation compilation = await project.GetCompilationAsync();
        var input = new CompressAttributeInfo
        {
            ContainingType = default,
            MemberName = "Value",
            MemberKind = MemberKind.Property,
            Accessibility = "public",
            IsPartial = true,
            ReturnType = ReturnType.String,
            AttributeType = CompressAttributeType.CompressFile,
            Location = default,
            SourceValue = source,
        };

        bool success = new CSharpRewriterTransformer(options).TryTransform(
            input,
            compilation,
            out string? output,
            out ImmutableArray<DiagnosticInfo> diagnostics,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectSuccess, success);
        if (expectSuccess)
        {
            Assert.NotNull(output);
        }

        if (!expectSuccess)
        {
            Assert.Null(output);
        }

        return (output, diagnostics);
    }
}
