using System.Collections.Immutable;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gori.CompressedStatics.Testing;

public sealed class ScopedConfigurationApplicationTests
{
    [Fact]
    public async Task PreservesGenericArgumentsNestedTypesAndResolvedTypes()
    {
        const string source = """
            using System;
            public class Payload
            {
                public Missing<DateTime>.Nested<int>[] Values;
                public DateTime Resolved;
            }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Container", "Missing"), new("Wrong.DateTime", "DateTime")]),
        });
        Assert.Empty(issues);
        Assert.Contains("global::Destination.Container<global::System.DateTime>.Nested<int>[]", result);
        Assert.Contains("global::System.DateTime Resolved", result);
        Assert.DoesNotContain("Wrong.DateTime", result);
        Assert.DoesNotContain("using System;", result);
    }

    public static TheoryData<string?, bool> IgnoreFilters
        => [
            new(null, true) { Label = "Unfiltered ignore reaches nested types" },
            new("Missing", true) { Label = "Matching message filter" },
            new("Different", false) { Label = "Message filter must match" },
        ];

    [Theory]
    [MemberData(nameof(IgnoreFilters))]
    public async Task UsesExplicitIgnoresForIssuesAndImportCleanup(string? filter, bool removesImports)
    {
        const string source = "using System; public class Outer { public class Nested { public Missing Value; } }";
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([], [new("CS0246", filter)]),
        });
        Assert.Equal(removesImports, !result.Contains("using System;", StringComparison.Ordinal));
        if (removesImports)
        {
            Assert.Empty(issues);
            return;
        }

        Unresolvable issue = Assert.IsType<Unresolvable>(Assert.Single(issues));
        Assert.Equal(UnresolvableType.UnresolvableType, issue.Type);
        Assert.Equal("Missing", issue.Name);
        Assert.NotNull(issue.Location);
        Assert.Equal("Missing", issue.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(issue.Location.SourceSpan));
    }

    [Fact]
    public async Task MappingDoesNotHideUnknownGenericArgumentsOrUnrelatedErrors()
    {
        const string source = """
            using System;
            public class Payload { public Missing<Unknown> Value; public int Broken() => "wrong"; }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Container", "Missing")]),
        });
        Assert.Contains("using System;", result);
        _ = Assert.Single(issues.OfType<Unresolvable>(), issue => issue.Name == "Unknown");
        _ = Assert.Single(issues.OfType<BindingIssue>(), issue => issue.DiagnosticID == "CS0029");
    }

    public static TheoryData<string, string[], bool> ExtensionPatterns
        => [
            new("value.Missing(1)", ["Missing(*, int)"], true) { Label = "Exact known parameter" },
            new("value.Missing(1)", ["Missing(*, string)"], false) { Label = "Typed parameter mismatch" },
            new("value.Missing(unknown)", ["Missing(*, int)"], false) { Label = "Typed parameter rejects unknown" },
            new("value.Missing<int>(1)", ["Missing(*, ...)"], true) { Label = "Explicit generic arguments" },
            new("value.Missing(1)", ["Missing"], true) { Label = "Name only" },
            new("value?.Missing(1)", ["Missing"], false) { Label = "Conditional access preserved" },
        ];

    [Theory]
    [MemberData(nameof(ExtensionPatterns))]
    public async Task AppliesExplicitExtensionPatterns(string invocation, string[] patterns, bool rewritten)
    {
        string source = $$"""
            using System;
            public class Payload { public object Run(object value) => {{invocation}}; }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            RewriteExtensionCalls = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Extensions") { ExtensionMethods = [.. patterns.Select(pattern => new ExtensionDefinition(pattern))] }]),
        });
        Assert.Equal(rewritten, result.Contains("global::Destination.Extensions.Missing", StringComparison.Ordinal));
        if (rewritten)
        {
            Assert.Empty(issues);
            Assert.DoesNotContain("using System;", result);
            Assert.Contains(invocation.Contains("<int>", StringComparison.Ordinal) ? "Missing<int>(value,1)" : "Missing(value,1)", result);
        }
        else
        {
            Assert.NotEmpty(issues);
            Assert.Contains("using System;", result);
        }
    }

    [Fact]
    public async Task ReportsAmbiguousExtensionConfigurationWithoutChoosingDestination()
    {
        const string source = "public class Payload { public object Run(object value) => value.Missing(); }";
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteExtensionCalls = true,
            Configuration = RewriterTestConfiguration.Create([
                new("One.Extensions", "First") { ExtensionMethods = [new("Missing")] },
                new("Two.Extensions", "Second") { ExtensionMethods = [new("Missing")] },
            ]),
        });
        Assert.Contains("value.Missing()", result);
        _ = Assert.Single(issues.OfType<InvalidConfiguration>(), issue => issue.Reason == InvalidConfigurationReason.ConflictingRules);
        _ = Assert.Single(issues.OfType<Unresolvable>(), issue => issue.Type == UnresolvableType.UnresolvableExtension);
    }

    [Fact]
    public async Task MovesMemberTransformsIntoTraversalWithoutDuplicatingExistingAttributes()
    {
        const string source = """
            using Generated = System.CodeDom.Compiler.GeneratedCodeAttribute;
            [Generated("Existing", "1")]
            public class Outer
            {
                public class Nested { }
                public delegate void Callback();
            }
            public enum State { Value }
            public delegate void Handler();
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewritePublicToInternal = true,
            AddAutoGeneratedAttribute = true,
            AddAutoGeneratedHeader = true,
            AddEmbeddedAttribute = true,
        });
        Assert.Empty(issues);
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(result, cancellationToken: TestContext.Current.CancellationToken).GetCompilationUnitRoot(TestContext.Current.CancellationToken);
        ClassDeclarationSyntax outer = Assert.Single(root.Members.OfType<ClassDeclarationSyntax>());
        Assert.Contains(outer.Modifiers, modifier => modifier.IsKind(SyntaxKind.InternalKeyword));
        Assert.Contains(Assert.Single(outer.Members.OfType<ClassDeclarationSyntax>()).Modifiers, modifier => modifier.IsKind(SyntaxKind.PublicKeyword));
        Assert.Contains(Assert.Single(outer.Members.OfType<DelegateDeclarationSyntax>()).Modifiers, modifier => modifier.IsKind(SyntaxKind.PublicKeyword));
        Assert.Equal(2, outer.AttributeLists.SelectMany(list => list.Attributes).Count());
        Assert.Equal(3, root.DescendantNodes().OfType<AttributeSyntax>().Count(attribute => attribute.Name.ToString() == "global::Microsoft.CodeAnalysis.Embedded"));
        Assert.Empty(outer.Members.OfType<ClassDeclarationSyntax>().SelectMany(type => type.AttributeLists));
        Assert.Empty(outer.Members.OfType<DelegateDeclarationSyntax>().SelectMany(type => type.AttributeLists));
        Assert.StartsWith("// <auto-generated />", result);
    }

    [Fact]
    public async Task MapsUnknownArgumentsInsideKnownGenericTypes()
    {
        const string source = """
            public class Payload { public System.Collections.Generic.List<Missing> Values; }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Context", "Missing")]),
        });
        Assert.Empty(issues);
        Assert.Contains("global::System.Collections.Generic.List<global::Destination.Context>", result);
    }

    [Fact]
    public async Task MapsAmbiguousTypesAndAttributeShortNames()
    {
        const string source = """
            using One;
            using Two;
            [Missing]
            public class Payload { public Context Value; }
            namespace One { public class Context { } }
            namespace Two { public class Context { } }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Context"), new("Destination.MissingAttribute")]),
        });
        Assert.Empty(issues);
        Assert.Contains("global::Destination.Context Value", result);
        Assert.Contains("[global::Destination.MissingAttribute]", result);
        Assert.DoesNotContain("using One;", result);
        Assert.DoesNotContain("using Two;", result);
    }

    [Fact]
    public async Task ReportsAmbiguousTypesWithoutMappings()
    {
        const string source = """
            using One;
            using Two;
            public class Payload { public Context Value; }
            namespace One { public class Context { } }
            namespace Two { public class Context { } }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter { RewriteTypeNames = true });
        Assert.Equal(UnresolvableType.AmbiguousType, Assert.IsType<Unresolvable>(Assert.Single(issues)).Type);
        Assert.Contains("using One;", result);
    }

    [Fact]
    public async Task MappedOutputCompilesAgainstDestinationDefinitions()
    {
        const string source = """
            using System;
            [Missing]
            public class Payload
            {
                public Ghost<int>.Nested<string> Value;
                public DateTime Run(object value) => value.Transform<DateTime>("text");
            }
            """;
        const string destination = """
            namespace Destination
            {
                public sealed class TagAttribute : System.Attribute { }
                public class Container<T> { public class Nested<U> { } }
                public static class Extensions
                {
                    public static T Transform<T>(object value, string text) => default(T);
                }
            }
            """;
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        SyntaxTree destinationTree = CSharpSyntaxTree.ParseText(destination, parseOptions, cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(destinationTree);
        var rewriter = new CSharpRewriter
        {
            AddEmbeddedAttribute = false,
            RewriteTypeNames = true,
            RewriteExtensionCalls = true,
            Configuration = RewriterTestConfiguration.Create([
                new("Destination.TagAttribute", "Missing"),
                new("Destination.Container", "Ghost"),
                new("Destination.Extensions") { ExtensionMethods = [new("Transform(*, string)")] },
            ]),
        };
        string result = rewriter.Rewrite(source, compilation, out IEnumerable<RewriteIssue> issues, TestContext.Current.CancellationToken);
        Assert.Empty(issues);
        Assert.Contains("global::Destination.Extensions.Transform<global::System.DateTime>(value,\"text\")", result);
        Assert.DoesNotContain("using System;", result);
        SyntaxTree original = Assert.Single(compilation.SyntaxTrees, tree => tree.GetText(TestContext.Current.CancellationToken).ToString() == source);
        Compilation rewritten = compilation.ReplaceSyntaxTree(original, CSharpSyntaxTree.ParseText(result, parseOptions, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(rewritten.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    public static TheoryData<string> TuplePatterns
        => [
            new("Transform(*, (int, (string, int)))") { Label = "Unnamed definition" },
            new("Transform(*, (int Other, (string Label, int Length) More))") { Label = "Different definition names" },
        ];

    [Theory]
    [MemberData(nameof(TuplePatterns))]
    public async Task IgnoresTupleNamesWhenMatchingAndRetainsSourceNames(string pattern)
    {
        const string source = """
            using System;
            public class Payload
            {
                public object Run(object value, (int Count, (Uri Address, int Size) Details) input)
                    => value.Transform((input.Count, (input.Details.Address.ToString(), input.Details.Size)));
            }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteTypeNames = true,
            RewriteExtensionCalls = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Extensions") { ExtensionMethods = [new(pattern)] }]),
        });
        Assert.Empty(issues);
        Assert.Contains("global::Destination.Extensions.Transform(value,", result);
        Assert.Contains("(int Count, (global::System.Uri Address, int Size) Details) input", result);
        Assert.Contains("input.Details.Address.ToString()", result);
        Assert.DoesNotContain("using System;", result);
    }

    public static TheoryData<string, string, bool> WrappedTuplePatterns
        => [
            new("(int Count, string Name)[]", "(int Other, string Label)[]", true) { Label = "Tuple array" },
            new("System.Collections.Generic.List<(int Count, string Name)>", "System.Collections.Generic.List<(int Other, string Label)>", true) { Label = "Tuple generic argument" },
            new("(int Count, string Name)[]", "(long Other, string Label)[]", false) { Label = "Element type mismatch" },
        ];

    [Theory]
    [MemberData(nameof(WrappedTuplePatterns))]
    public async Task MatchesWrappedTuplesByElementTypes(string type, string pattern, bool matches)
    {
        string source = $$"""
            public class Payload { public object Run(object value, {{type}} input) => value.Transform(input); }
            """;
        (string result, RewriteIssue[] issues) = await Rewrite(source, new CSharpRewriter
        {
            RewriteExtensionCalls = true,
            Configuration = RewriterTestConfiguration.Create([new("Destination.Extensions") { ExtensionMethods = [new("Transform(*, " + pattern + ")")] }]),
        });
        Assert.Equal(matches, result.Contains("global::Destination.Extensions.Transform(value,input)", StringComparison.Ordinal));
        Assert.Equal(matches, issues.Length == 0);
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
