using System.Collections.Immutable;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gori.CompressedStatics.Testing;

public sealed class RewriterReviewRegressionTests(ITestOutputHelper output)
{
    public static TheoryData<MockSourceData> ImplicitExtensionCases
        => [
            new(new MockSourceData
            {
                SourceCode = """
                    using QueryExtensions;
                    public static class Sample
                    {
                        public static int Read()
                        {
                            var query = from value in new[] { 4 } select value + 1;
                            return query[0];
                        }
                    }
                    namespace QueryExtensions
                    {
                        public static class Helpers
                        {
                            public static int[] Select(this int[] values, System.Func<int, int> selector)
                                => new[] { selector(values[0]) };
                        }
                    }
                    """,
            }) { Label = "Custom query operator outside System.Linq" },
            new(new MockSourceData
            {
                SourceCode = """
                    using EnumerationExtensions;
                    public sealed class Payload { }
                    public static class Sample
                    {
                        public static int Read()
                        {
                            foreach (int value in new Payload())
                                return value;
                            return 0;
                        }
                    }
                    namespace EnumerationExtensions
                    {
                        public static class Helpers
                        {
                            public static System.Collections.Generic.List<int>.Enumerator GetEnumerator(this Payload value)
                                => new System.Collections.Generic.List<int> { 4 }.GetEnumerator();
                        }
                    }
                    """,
            }) { Label = "Extension GetEnumerator in foreach" },
            new(new MockSourceData
            {
                SourceCode = """
                    using AwaitExtensions;
                    public sealed class Payload { }
                    public static class Sample
                    {
                        public static async System.Threading.Tasks.Task<int> Read() => await new Payload();
                    }
                    namespace AwaitExtensions
                    {
                        public static class Helpers
                        {
                            public static System.Runtime.CompilerServices.TaskAwaiter<int> GetAwaiter(this Payload value)
                                => System.Threading.Tasks.Task.FromResult(4).GetAwaiter();
                        }
                    }
                    """,
            }) { Label = "Extension GetAwaiter in await" },
            new(new MockSourceData
            {
                SourceCode = """
                    using DeconstructionExtensions;
                    public sealed class Payload { }
                    public static class Sample
                    {
                        public static int Read()
                        {
                            var (first, second) = new Payload();
                            return first + second;
                        }
                    }
                    namespace DeconstructionExtensions
                    {
                        public static class Helpers
                        {
                            public static void Deconstruct(this Payload value, out int first, out int second)
                            {
                                first = 4;
                                second = 5;
                            }
                        }
                    }
                    """,
            }) { Label = "Extension Deconstruct in deconstruction" },
            new(new MockSourceData
            {
                SourceCode = """
                    using AsyncEnumerationExtensions;
                    public sealed class Payload { }
                    public static class Sample
                    {
                        public static async System.Threading.Tasks.Task<int> Read()
                        {
                            await foreach (int value in new Payload()) return value;
                            return 0;
                        }
                    }
                    namespace AsyncEnumerationExtensions
                    {
                        public static class Helpers
                        {
                            public static System.Collections.Generic.IAsyncEnumerator<int> GetAsyncEnumerator(this Payload value)
                                => Values().GetAsyncEnumerator();
                            private static async System.Collections.Generic.IAsyncEnumerable<int> Values()
                            {
                                await System.Threading.Tasks.Task.Yield();
                                yield return 4;
                            }
                        }
                    }
                    """,
            }) { Label = "Extension GetAsyncEnumerator in await foreach" },
            new(new MockSourceData
            {
                SourceCode = """
                    using OuterExtensions;
                    using InnerExtensions;
                    public sealed class Payload { }
                    public sealed class Pair { }
                    public static class Sample
                    {
                        public static int Read()
                        {
                            int first, second, third;
                            ((first, second), third) = new Payload();
                            foreach (var ((a, b), c) in new[] { new Payload() })
                                return first + second + third + a + b + c;
                            return 0;
                        }
                    }
                    namespace OuterExtensions
                    {
                        public static class Helpers
                        {
                            public static void Deconstruct(this Payload value, out Pair pair, out int last)
                            { pair = new Pair(); last = 3; }
                        }
                    }
                    namespace InnerExtensions
                    {
                        public static class Helpers
                        {
                            public static void Deconstruct(this Pair value, out int first, out int second)
                            { first = 1; second = 2; }
                        }
                    }
                    """,
            }) { Label = "Nested extension deconstruction in assignment and foreach" },
            new(new MockSourceData
            {
                SourceCode = """
                    using QueryExtensions;
                    public static class Sample
                    {
                        public static object Read()
                            => from value in new[] { 4 } where value > 0 orderby value, value descending group value by value;
                    }
                    namespace QueryExtensions
                    {
                        public static class Helpers
                        {
                            public static System.Collections.Generic.IEnumerable<T> Where<T>(this System.Collections.Generic.IEnumerable<T> values, System.Func<T, bool> predicate)
                                => System.Linq.Enumerable.Where(values, predicate);
                            public static System.Linq.IOrderedEnumerable<T> OrderBy<T, K>(this System.Collections.Generic.IEnumerable<T> values, System.Func<T, K> key)
                                => System.Linq.Enumerable.OrderBy(values, key);
                            public static System.Linq.IOrderedEnumerable<T> ThenByDescending<T, K>(this System.Linq.IOrderedEnumerable<T> values, System.Func<T, K> key)
                                => System.Linq.Enumerable.ThenByDescending(values, key);
                            public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<K, T>> GroupBy<T, K>(this System.Collections.Generic.IEnumerable<T> values, System.Func<T, K> key)
                                => System.Linq.Enumerable.GroupBy(values, key);
                        }
                    }
                    """,
            }) { Label = "Custom where ordering and group query operators" },
        ];

    [Theory]
    [MemberData(nameof(ImplicitExtensionCases))]
    public async Task TypeQualificationPreservesImplicitExtensionDependencies(MockSourceData data)
        => await AssertImplicitExtensionImports(data, CSharpRewriterOptions.RewriteTypeNames);

    [Theory]
    [MemberData(nameof(ImplicitExtensionCases))]
    public async Task ExtensionCallRewritingPreservesImplicitExtensionDependencies(MockSourceData data)
        => await AssertImplicitExtensionImports(data, CSharpRewriterOptions.RewriteTypeNames | CSharpRewriterOptions.RewriteExtensionCalls);

    private async Task AssertImplicitExtensionImports(MockSourceData data, CSharpRewriterOptions options)
    {
        Compilation rewritten = await RewriteValidSource(new MockSourceData
        {
            SourceCode = "using System.Text;\n" + data.SourceCode,
        }, options);
        string source = Assert.Single(rewritten.SyntaxTrees).ToString();
        Assert.DoesNotContain("using System.Text;", source, StringComparison.Ordinal);
        foreach (UsingDirectiveSyntax directive in CSharpSyntaxTree.ParseText(data.SourceCode!).GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            Assert.Contains(directive.ToString(), source, StringComparison.Ordinal);
        }
    }

    public static TheoryData<string, string, string, string> NameofAliasCases
        => [
            new("Text", "System.String", "nameof(Text)", "Text") { Label = "Simple type alias" },
            new("NumberList", "System.Collections.Generic.List<int>", "nameof(NumberList)", "NumberList") { Label = "Constructed generic type alias" },
            new("@class", "System.String", "nameof(@class)", "class") { Label = "Escaped type alias" },
            new("Text", "System.String", "nameof(Text.Empty)", "Empty") { Label = "Member accessed through alias" },
            new("Text", "System.String", "nameof(Text) + \":\" + nameof(Text.Empty)", "Text:Empty") { Label = "Alias and member names in enclosing expression" },
            new("NumberList", "System.Collections.Generic.List<int>", "nameof(NumberList.Count)", "Count") { Label = "Member of generic type alias" },
        ];

    [Theory]
    [MemberData(nameof(NameofAliasCases))]
    public async Task TypeQualificationPreservesNameofAliasValue(string alias, string type, string expression, string expected)
    {
        var data = new MockSourceData
        {
            SourceCode = $$"""
                using {{alias}} = {{type}};
                public static class Sample
                {
                    public const string Value = {{expression}};
                    public static System.Type ReadType() => typeof({{alias}});
                }
                """,
        };
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, data, out _, cancellationToken: TestContext.Current.CancellationToken);
        Compilation original = await project.GetCompilationAsync();
        Assert.Equal(expected, GetValue(original));
        Compilation rewritten = await RewriteValidSource(data, CSharpRewriterOptions.RewriteTypeNames);
        Assert.Equal(expected, GetValue(rewritten));
        Assert.Contains($"typeof({alias})", Assert.Single(rewritten.SyntaxTrees).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrdinaryMethodNamedNameofStillQualifiesTypes()
    {
        Compilation rewritten = await RewriteValidSource(new MockSourceData
        {
            SourceCode = """
                using Text = System.String;
                public static class Sample
                {
                    public static System.Type @nameof(System.Type value) => value;
                    public static System.Type Read() => nameof(typeof(Text));
                }
                """,
        }, CSharpRewriterOptions.RewriteTypeNames);
        Assert.Contains("nameof(typeof(Text))", Assert.Single(rewritten.SyntaxTrees).ToString(), StringComparison.Ordinal);
    }

    public static TheoryData<string, string, string> AliasReferenceCases
        => [
            new("using Map = System.Collections.Generic.Dictionary<int, int>;", "Map.KeyCollection", "Map.KeyCollection") { Label = "Nested type through type alias" },
            new("using Collections = System.Collections.Generic;", "Collections.List<StringBuilder>", "Collections.List<global::System.Text.StringBuilder>") { Label = "Namespace alias with independent generic argument" },
            new("using Collections = System.Collections.Generic;", "Collections::List<StringBuilder>", "Collections::List<global::System.Text.StringBuilder>") { Label = "Namespace alias qualifier" },
            new("global using Text = System.String;", "Text", "Text") { Label = "Global alias declared within source" },
        ];

    [Theory]
    [MemberData(nameof(AliasReferenceCases))]
    public async Task TypeQualificationPreservesAliasChains(string directive, string type, string expected)
    {
        Compilation rewritten = await RewriteValidSource(new MockSourceData
        {
            SourceCode = $$"""
                {{directive}}
                using System.Text;
                public static class Sample
                {
                    public static {{type}} Read({{type}} value) => value;
                    public static System.Type ReadType() => typeof({{type}});
                }
                """,
        }, CSharpRewriterOptions.RewriteTypeNames);
        string source = Assert.Single(rewritten.SyntaxTrees).ToString();
        Assert.Contains(directive, source, StringComparison.Ordinal);
        Assert.Contains($"{expected} Read({expected} value)", source, StringComparison.Ordinal);
        Assert.Contains($"typeof({expected})", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using System.Text;", source, StringComparison.Ordinal);
    }

    public static TheoryData<string, string, bool> AttributeIdentityCases
        => [
            new("Custom.GeneratedCode", "Custom.Embedded", false) { Label = "Custom qualified names" },
            new("GeneratedCode", "Embedded", false) { Label = "Custom short names" },
            new("GC", "EM", false) { Label = "Aliases to custom types" },
            new("RealGC(\"Existing\", \"1\")", "RealEM", false) { Label = "Aliases to real attributes" },
            new("Custom.GeneratedCode", "Custom.Embedded", true) { Label = "Custom names after type qualification" },
            new("GeneratedCode", "Embedded", true) { Label = "Custom short names after type qualification" },
            new("RealGC(\"Existing\", \"1\")", "RealEM", true) { Label = "Real aliases after type qualification" },
        ];

    [Theory]
    [MemberData(nameof(AttributeIdentityCases))]
    public async Task MarkerAttributesUseResolvedIdentity(string generatedAttribute, string embeddedAttribute, bool qualifyTypes)
    {
        string source = $$"""
            using Custom;
            using GC = Custom.GeneratedCodeAttribute;
            using EM = Custom.EmbeddedAttribute;
            using RealGC = System.CodeDom.Compiler.GeneratedCodeAttribute;
            using RealEM = Microsoft.CodeAnalysis.EmbeddedAttribute;
            [{{generatedAttribute}}, {{embeddedAttribute}}]
            public class Sample
            {
                [{{generatedAttribute}}, {{embeddedAttribute}}]
                public class Nested { }
            }
            [{{generatedAttribute}}, {{embeddedAttribute}}]
            public delegate void Callback();
            namespace Custom
            {
                public sealed class GeneratedCodeAttribute : System.Attribute { }
                public sealed class EmbeddedAttribute : System.Attribute { }
            }
            namespace Microsoft.CodeAnalysis
            {
                internal sealed class EmbeddedAttribute : System.Attribute { }
            }
            """;
        var data = new MockSourceData { SourceCode = source };
        CSharpRewriterOptions options = CSharpRewriterOptions.AddAutoGeneratedAttribute | CSharpRewriterOptions.AddEmbeddedAttribute;
        if (qualifyTypes)
        {
            options |= CSharpRewriterOptions.RewriteTypeNames;
        }

        Compilation rewritten = await RewriteValidSource(data, options);
        foreach (string typeName in new[] { "Sample", "Callback" })
        {
            INamedTypeSymbol type = Assert.IsType<INamedTypeSymbol>(rewritten.GetTypeByMetadataName(typeName), exactMatch: false);
            AttributeData[] attributes = [.. type.GetAttributes()];
            _ = Assert.Single(attributes, attribute => attribute.AttributeClass?.ToDisplayString() == "System.CodeDom.Compiler.GeneratedCodeAttribute");
            _ = Assert.Single(attributes, attribute => attribute.AttributeClass?.ToDisplayString() == "Microsoft.CodeAnalysis.EmbeddedAttribute");
            Assert.Equal(generatedAttribute.StartsWith("RealGC", StringComparison.Ordinal) ? 2 : 4, attributes.Length);
        }

        INamedTypeSymbol nested = Assert.IsType<INamedTypeSymbol>(rewritten.GetTypeByMetadataName("Sample+Nested"), exactMatch: false);
        AttributeData[] nestedAttributes = [.. nested.GetAttributes()];
        Assert.Equal(2, nestedAttributes.Length);
        string expectedGenerated = generatedAttribute.StartsWith("RealGC", StringComparison.Ordinal)
            ? "System.CodeDom.Compiler.GeneratedCodeAttribute" : "Custom.GeneratedCodeAttribute";
        string expectedEmbedded = generatedAttribute.StartsWith("RealGC", StringComparison.Ordinal)
            ? "Microsoft.CodeAnalysis.EmbeddedAttribute" : "Custom.EmbeddedAttribute";
        _ = Assert.Single(nestedAttributes, attribute => attribute.AttributeClass?.ToDisplayString() == expectedGenerated);
        _ = Assert.Single(nestedAttributes, attribute => attribute.AttributeClass?.ToDisplayString() == expectedEmbedded);

        string transformed = Assert.Single(rewritten.SyntaxTrees).ToString();
        Compilation repeated = await RewriteValidSource(new MockSourceData { SourceCode = transformed }, options);
        Assert.Equal(transformed, Assert.Single(repeated.SyntaxTrees).ToString());
    }

    private async Task<Compilation> RewriteValidSource(MockSourceData data, CSharpRewriterOptions options)
    {
        var project = MockProject.Create(typeof(CompressFileAttribute).Assembly, data, out Document document,
            cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        SyntaxTree tree = (await document.GetSyntaxTreeAsync(TestContext.Current.CancellationToken))!;
        var input = new CompressAttributeInfo
        {
            ContainingType = default,
            MemberName = "Test",
            MemberKind = MemberKind.Property,
            Accessibility = "public",
            IsPartial = true,
            ReturnType = ReturnType.String,
            AttributeType = CompressAttributeType.CompressFile,
            Location = default,
            SourceValue = data.SourceCode,
        };
        var transformer = new CSharpRewriterTransformer(options);
        bool success = transformer.TryTransform(input, compilation, out string? transformed, out ImmutableArray<DiagnosticInfo> diagnostics,
            TestContext.Current.CancellationToken);
        output.WriteLine(transformed ?? "<No Code>");
        if (!diagnostics.IsDefaultOrEmpty)
        {
            output.Write(diagnostics.Select(item => item.ToDiagnostic(Location.None)));
        }

        Assert.True(success);
        Assert.True(diagnostics.IsDefaultOrEmpty);
        Assert.NotNull(transformed);
        SyntaxTree rewrittenTree = CSharpSyntaxTree.ParseText(transformed, (CSharpParseOptions)tree.Options,
            path: tree.FilePath, cancellationToken: TestContext.Current.CancellationToken);
        Compilation rewritten = compilation.ReplaceSyntaxTree(tree, rewrittenTree);
        Assert.Empty(rewritten.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error));
        return rewritten;
    }

    private static object? GetValue(Compilation compilation)
    {
        SyntaxTree tree = Assert.Single(compilation.SyntaxTrees);
        VariableDeclaratorSyntax field = Assert.Single(tree.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<VariableDeclaratorSyntax>());
        IFieldSymbol symbol = Assert.IsType<IFieldSymbol>(compilation.GetSemanticModel(tree)
            .GetDeclaredSymbol(field, TestContext.Current.CancellationToken), exactMatch: false);
        Assert.True(symbol.HasConstantValue);
        return symbol.ConstantValue;
    }
}
