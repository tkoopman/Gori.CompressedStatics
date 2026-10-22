using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;

using Gori.Common.Roslyn;
using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Testing;

public sealed class TransformerConstructorTests
{
    public static TheoryData<string, string> ConstructorCases
        => [
            new("[\"prefix:\"]", "prefix:payload") { Label = "Exact string beats assignable object" },
            new("[7]", "object:payload") { Label = "Assignable object constructor" },
            new("[null]", "null:payload") { Label = "Null chooses most-specific reference" },
            new("[]", "constructed:payload") { Label = "Explicit empty bypasses Instance" },
            new("new object[] { \"prefix:\" }", "prefix:payload") { Label = "Explicit array initializer remains supported" },
        ];

    public static TheoryData<string, Type, string> InvalidCases
        => [
            new("new object[] { 7, 8 }", typeof(ConfigurableTransformer), "GCS030") { Label = "Wrong argument count" },
            new("new object[] { 7 }", typeof(LongTransformer), "GCS030") { Label = "No numeric coercion" },
            new("new object[] { }", typeof(LongTransformer), "GCS030") { Label = "No optional argument filling" },
            new("new object[] { \"a\", \"b\" }", typeof(ParamsTransformer), "GCS030") { Label = "No params expansion" },
            new("new object[] { 7 }", typeof(AmbiguousTransformer), "GCS031") { Label = "Ambiguous unrelated interfaces" },
            new("new object[] { null }", typeof(AmbiguousNullTransformer), "GCS031") { Label = "Ambiguous unrelated null targets" },
            new("new object[] { \"bad\" }", typeof(ThrowingTransformer), "GCS025") { Label = "Throwing constructor" },
            new("new object[] { 7 }", typeof(PrivateTransformer), "GCS030") { Label = "Private constructor excluded" },
            new("new object[] { typeof(Sample) }", typeof(ConfigurableTransformer), "GCS032") { Label = "Consumer typeof cannot load" },
        ];

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public async Task GeneratorInvokesExplicitConstructor(string arguments, string expected)
    {
        MockProject project = CreateProject(arguments, typeof(ConfigurableTransformer));
        Assert.Equal(expected, await GenerateAndReadValue(project));
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public async Task ConstructorFailureProducesExplicitDiagnostic(string arguments, Type transformerType, string expectedId)
    {
        MockProject project = CreateProject(arguments, transformerType);
        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        SyntaxTree tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == diagnostic.Location.GetLineSpan().Path);
        Assert.StartsWith("CompressFile(", tree.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OmittedArgumentsPreserveSingletonSelection()
        => Assert.Equal("singleton:payload", await GenerateAndReadValue(CreateProject(null, typeof(ConfigurableTransformer))));

    [Fact]
    public async Task EnumTypeofAndNestedArraysMaterializeWithRuntimeTypes()
    {
        MockProject project = CreateProject("""
            new object[] {
                System.DayOfWeek.Friday,
                typeof(System.Collections.Generic.Dictionary<string, int[]>),
                new object[] {
                    new int[] { 2, 3 },
                    new System.DayOfWeek[] { System.DayOfWeek.Monday },
                    typeof(System.Environment.SpecialFolder),
                    null,
                    new object[] { "nested", true },
                    new string[] { }
                }
            }
            """, typeof(ComplexTransformer));
        Assert.Equal("complex:payload", await GenerateAndReadValue(project));
    }

    [Fact]
    public async Task BinaryConstructorArgumentsReachGeneratorAndLoader()
    {
        MockProject project = CreateProject("new object[] { new byte[] { 1, 2, 255 } }", typeof(BinaryTransformer));
        Assert.Equal(new byte[] { 1, 2, 255 }, await GenerateAndReadValue<byte[]>(project));
        Compilation compilation = await project.GetCompilationAsync();
        AttributeData attribute = Assert.Single(compilation.GetTypeByMetadataName("Sample")!.GetMembers("Value")[0].GetAttributes());
        TypedConstant array = attribute.NamedArguments.Single(argument => argument.Key == nameof(CompressFileAttribute.TransformerArguments)).Value;
        ImmutableArray<TransformerArgument> arguments = [.. array.Values.Select(TransformerArgument.FromConstant)];
        Assert.True(ValueTransformerHelper.TryGetTransformer(typeof(BinaryTransformer).AssemblyQualifiedName!, arguments, out IBinaryTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.Equal(new byte[] { 1, 2, 255 }, Assert.IsType<BinaryTransformer>(transformer).Bytes);
    }

    [Fact]
    public async Task ExplicitEmptyArgumentsDoNotReadImplicitInstance()
        => Assert.Equal("constructed:payload", await GenerateAndReadValue(CreateProject("new object[] { }", typeof(ThrowingSingletonTransformer))));

    [Fact]
    public async Task ArgumentSnapshotsAndMemberCopiesHaveStructuralEquality()
    {
        const string expression = "new object[] { System.DayOfWeek.Friday, typeof(string), new object[] { new int[] { 1, 2 }, null } }";
        ImmutableArray<TransformerArgument> first = await ExtractArguments(expression);
        ImmutableArray<TransformerArgument> second = await ExtractArguments(expression);
        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.Equal(first.Select(argument => argument.GetHashCode()), second.Select(argument => argument.GetHashCode()));
        Assert.NotEqual(first[2], (await ExtractArguments("new object[] { new object[] { new int[] { 1, 3 }, null } }"))[0]);

        CompressAttributeInfo member = CreateMember(first);
        CompressAttributeInfo same = CreateMember(second);
        Assert.Equal(member, same);
        Assert.Equal(member.GetHashCode(), same.GetHashCode());
        Assert.Equal(first.ToArray(), member.WithSourceValue("replacement").TransformerArguments.ToArray());
        Assert.Equal(first.ToArray(), member.WithSourceBytes([1, 2]).TransformerArguments.ToArray());
        Assert.Equal(first.ToArray(), member.Invalidate().TransformerArguments.ToArray());
        Assert.NotEqual(CreateMember(default), CreateMember([]));
        Assert.NotEqual(member, CreateMember(await ExtractArguments("new object[] { 1 }")));
    }

    [Fact]
    public void StaticMemberSelectionIsPreservedByCopiesAndEquality()
    {
        CompressAttributeInfo original = CreateMember(default);
        CompressAttributeInfo selected = original with { TransformerMember = "Selected" };
        CompressAttributeInfo same = original with { TransformerMember = "Selected" };
        CompressAttributeInfo alternate = original with { TransformerMember = "Alternate" };

        Assert.Equal(selected, same);
        Assert.Equal(selected.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(original, selected);
        Assert.NotEqual(selected, alternate);
        Assert.Equal("Selected", selected.WithSourceValue("replacement").TransformerMember);
        Assert.Equal("Selected", selected.WithSourceBytes([1, 2]).TransformerMember);
        Assert.Equal("Selected", selected.Invalidate().TransformerMember);
    }

    public static TheoryData<string, bool, bool> StaticMemberCases
        => [
            new("CompressFile", false, false) { Label = "File string property" },
            new("CompressFile", true, false) { Label = "File string method" },
            new("CompressString", false, false) { Label = "String property" },
            new("CompressString", true, false) { Label = "String method" },
            new("CompressFile", false, true) { Label = "File binary property" },
            new("CompressFile", true, true) { Label = "File binary method" },
            new("CompressString", false, true) { Label = "String binary property" },
            new("CompressString", true, true) { Label = "String binary method" },
        ];

    [Theory]
    [MemberData(nameof(StaticMemberCases))]
    public async Task GeneratorPreservesExplicitStaticMember(string attribute, bool method, bool binary)
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument($$"""
            using Gori.CompressedStatics;
            public static partial class Sample
            {
                [{{attribute}}("{{(attribute == "CompressFile" ? "payload.txt" : "payload")}}", Transformer = {{SymbolDisplay.FormatLiteral(typeof(SelectedProvider).AssemblyQualifiedName + (binary ? "::Binary" : "::Selected"), true)}})]
                public static partial {{(binary ? "byte[]" : "string")}} Value{{(method ? "();" : " { get; }")}}
            }
            """, "src\\Sample.cs");
        _ = project.AddAdditionalFile("src\\payload.txt", "payload");
        if (binary)
        {
            Assert.Equal(new byte[] { 1, 2, 255 }, await GenerateAndReadValue<byte[]>(project));
        }
        else
        {
            Assert.Equal("selected:payload", await GenerateAndReadValue(project));
        }
    }

    [Fact]
    public async Task ChangingOnlyStaticMemberInvalidatesIncrementalInput()
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        string source = $$"""
            using Gori.CompressedStatics;
            public static partial class Sample
            {
                [CompressFile("payload.txt", Transformer = {{SymbolDisplay.FormatLiteral(typeof(SelectedProvider).AssemblyQualifiedName + "::Selected", true)}})]
                public static partial string Value { get; }
            }
            """;
        Document document = project.AddDocument(source, "src\\Sample.cs");
        _ = project.AddAdditionalFile("src\\payload.txt", "payload");
        Compilation compilation = await project.GetCompilationAsync();
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorRunResult original = Assert.Single(driver.GetRunResult().Results);
        Assert.Empty(original.Diagnostics);
        Assert.Null(original.Exception);

        document = document.WithText(SourceText.From(source.Replace("::Selected", "::Alternate", StringComparison.Ordinal)));
        compilation = (await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorRunResult updated = Assert.Single(driver.GetRunResult().Results);
        Assert.Empty(updated.Diagnostics);
        Assert.Null(updated.Exception);
        (object value, IncrementalStepRunReason reason) = Assert.Single(updated.TrackedSteps[CompressStaticsGenerator.IndividualMemberTrackingName].SelectMany(step => step.Outputs));
        Assert.Equal(IncrementalStepRunReason.Modified, reason);
        (CompressAttributeInfo input, Supports _) = Assert.IsType<(CompressAttributeInfo, Supports)>(value);
        Assert.Equal("Alternate", input.TransformerMember);
        Assert.Equal("alternate:payload", input.SourceValue);
    }

    public static class SelectedProvider
    {
        public static readonly IStringTransformer Selected = new ConfigurableTransformer("selected:");

        public static readonly IBinaryTransformer Binary = new BinaryTransformer([1, 2, 255]);

        public static IStringTransformer Alternate { get; } = new ConfigurableTransformer("alternate:");
    }

    private static CompressAttributeInfo CreateMember(ImmutableArray<TransformerArgument> arguments)
        => new()
        {
            ContainingType = CSharpCompilation.Create("MemberTest", [CSharpSyntaxTree.ParseText("class Sample { }")]).GetTypeByMetadataName("Sample")!.CreateEquatableTypeInfo(),
            MemberName = "Value",
            MemberKind = MemberKind.Property,
            Accessibility = "public",
            IsPartial = true,
            ReturnType = ReturnType.String,
            AttributeType = CompressAttributeType.CompressFile,
            SourceValue = "payload",
            Transformer = typeof(ConfigurableTransformer).AssemblyQualifiedName,
            TransformerArguments = arguments,
            Location = default,
        };

    private static async Task<ImmutableArray<TransformerArgument>> ExtractArguments(string expression)
    {
        Compilation compilation = await CreateProject(expression, typeof(ConfigurableTransformer)).GetCompilationAsync();
        AttributeData attribute = Assert.Single(compilation.GetTypeByMetadataName("Sample")!.GetMembers("Value")[0].GetAttributes());
        TypedConstant array = attribute.NamedArguments.Single(argument => argument.Key == nameof(CompressFileAttribute.TransformerArguments)).Value;
        return [.. array.Values.Select(TransformerArgument.FromConstant)];
    }

    private static MockProject CreateProject(string? arguments, Type transformer, string suffix = "")
    {
        var project = new MockProject(typeof(CompressFileAttribute).Assembly, cancellationToken: TestContext.Current.CancellationToken);
        string argumentsProperty = arguments is null ? string.Empty : ", TransformerArguments = " + arguments;
        _ = project.AddDocument($$"""
            using Gori.CompressedStatics;
            public static partial class Sample
            {
                [CompressFile("payload.txt", Transformer = {{SymbolDisplay.FormatLiteral(transformer.AssemblyQualifiedName! + suffix, true)}}{{argumentsProperty}})]
                public static partial {{(transformer == typeof(BinaryTransformer) ? "byte[]" : "string")}} Value { get; }
            }
            """, "src\\Sample.cs");
        _ = project.AddAdditionalFile("src\\payload.txt", "payload");
        return project;
    }

    private static Task<string> GenerateAndReadValue(MockProject project) => GenerateAndReadValue<string>(project);

    private static async Task<T> GenerateAndReadValue<T>(MockProject project)
    {
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .AddGenerators([new DecompressStaticsGenerator().AsSourceGenerator()])
            .RunGeneratorsAndUpdateCompilation(await project.GetCompilationAsync(), out Compilation generated, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.All(driver.GetRunResult().Results, result => Assert.Null(result.Exception));
        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = generated.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(nameof(TransformerConstructorTests), isCollectible: true);
        try
        {
            Type sample = context.LoadFromStream(stream).GetType("Sample")!;
            object? value = sample.GetProperty("Value") is { } property
                ? property.GetValue(null)
                : sample.GetMethod("Value")!.Invoke(null, null);
            return Assert.IsType<T>(value);
        }
        finally
        {
            context.Unload();
        }
    }

    public class ConfigurableTransformer : IStringTransformer
    {
        public ConfigurableTransformer() => Prefix = "constructed:";

        public ConfigurableTransformer(string? prefix) => Prefix = prefix ?? "null:";

#pragma warning disable IDE0060 // Remove unused parameter
        public ConfigurableTransformer(object value) => Prefix = "object:";
#pragma warning restore IDE0060 // Remove unused parameter

        public static ConfigurableTransformer Instance { get; } = new("singleton:");

        public string Prefix { get; }

        public bool TryTransform(ICompressionAttributeInfo attribute, Compilation compilation, [NotNullWhen(true)] out string? output, [NotNullWhen(false)] out DiagnosticInfo? diagnostic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = Prefix + attribute.SourceValue;
            diagnostic = null;
            return true;
        }
    }

    public sealed class LongTransformer(long number = 1) : ConfigurableTransformer(number)
    {
    }

    public sealed class ParamsTransformer(params string[] values) : ConfigurableTransformer(values)
    {
    }

    public sealed class AmbiguousTransformer : ConfigurableTransformer
    {
        public AmbiguousTransformer(IComparable value) : base(value) { }

        public AmbiguousTransformer(IFormattable value) : base(value) { }
    }

    public sealed class AmbiguousNullTransformer : ConfigurableTransformer
    {
        public AmbiguousNullTransformer(string value) : base(value) { }

        public AmbiguousNullTransformer(Type value) : base(value) { }
    }

    public sealed class ThrowingTransformer : ConfigurableTransformer
    {
        public ThrowingTransformer(string value) => throw new InvalidOperationException(value);
    }

    public sealed class PrivateTransformer : ConfigurableTransformer
    {
        private PrivateTransformer(int value) : base(value) { }
    }

    public sealed class ThrowingSingletonTransformer : ConfigurableTransformer
    {
        public static new ThrowingSingletonTransformer Instance => throw new InvalidOperationException("Implicit singleton must not run.");
    }

    public sealed class BinaryTransformer(byte[] bytes) : IBinaryTransformer
    {
        public byte[] Bytes { get; } = bytes;

        public bool TryTransform(ICompressionAttributeInfo attribute, Compilation compilation, [NotNullWhen(true)] out byte[]? output, [NotNullWhen(false)] out DiagnosticInfo? diagnostic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = Bytes;
            diagnostic = null;
            return true;
        }
    }

    public sealed class ComplexTransformer : ConfigurableTransformer
    {
        public ComplexTransformer(DayOfWeek day, Type type, object[] values) : base("complex:")
        {
            Assert.Equal(DayOfWeek.Friday, day);
            Assert.Equal(typeof(Dictionary<string, int[]>), type);
            Assert.Equal([2, 3], Assert.IsType<int[]>(values[0]));
            Assert.Equal([DayOfWeek.Monday], Assert.IsType<DayOfWeek[]>(values[1]));
            Assert.Equal(typeof(Environment.SpecialFolder), values[2]);
            Assert.Null(values[3]);
            Assert.Equal(["nested", true], Assert.IsType<object[]>(values[4]));
            Assert.Empty(Assert.IsType<string[]>(values[5]));
        }
    }
}
