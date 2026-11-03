using Gori.Common.Roslyn.Test;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Testing;

public sealed class CSharpRewriterAttributeTests
{
    public static TheoryData<string> Targets
        => [
            new("class Payload { }") { Label = "Class" },
            new("struct Payload { }") { Label = "Struct" },
            new("record Payload;") { Label = "Record" },
            new("record struct Payload;") { Label = "Record struct" },
            new("interface Payload { }") { Label = "Interface" },
            new("enum Payload { Value }") { Label = "Enum" },
            new("delegate void Payload();") { Label = "Delegate" },
        ];

    [Theory]
    [MemberData(nameof(Targets))]
    public async Task RejectsNonAssemblyTargets(string target)
    {
        string source = $$"""
            using Gori.CompressedStatics;
            [CSharpRewriterIgnore("CS0656")]
            [CSharpRewriterTypeMapping("Library.Context")]
            public {{target}}
            """;
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();
        Diagnostic[] errors = [.. compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];

        Assert.Equal(2, errors.Length);
        Assert.All(errors, diagnostic => Assert.Equal("CS0592", diagnostic.Id));
    }

    [Fact]
    public async Task SupportsRepeatedAssemblyAttributesInRootNamespace()
    {
        const string source = """
            using Gori.CompressedStatics;
            [assembly: CSharpRewriterIgnore("CS0656")]
            [assembly: CSharpRewriterIgnore("CS0518", "IsExternalInit")]
            [assembly: CSharpRewriterTypeMapping("Library.Context")]
            [assembly: CSharpRewriterTypeMapping("Library.Extensions", "Old.Extensions", ExtensionMethods = ["Method(*, string, ...)", "Other"])]
            public class Payload { }
            """;
        var project = MockProject.Create(typeof(CSharpRewriterIgnoreAttribute).Assembly, source, cancellationToken: TestContext.Current.CancellationToken);
        Compilation compilation = await project.GetCompilationAsync();

        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        AttributeData[] attributes = [.. compilation.Assembly.GetAttributes().Where(attribute => attribute.AttributeClass?.ContainingNamespace.ToDisplayString() == "Gori.CompressedStatics")];
        Assert.Equal(4, attributes.Length);
        Assert.Equal("Gori.CompressedStatics.CSharpRewriterIgnoreAttribute", attributes[0].AttributeClass!.ToDisplayString());
        Assert.Equal("CS0656", attributes[0].ConstructorArguments[0].Value);
        Assert.Null(attributes[0].ConstructorArguments[1].Value);
        Assert.Equal("IsExternalInit", attributes[1].ConstructorArguments[1].Value);
        Assert.Equal("Gori.CompressedStatics.CSharpRewriterTypeMappingAttribute", attributes[2].AttributeClass!.ToDisplayString());
        Assert.Equal("Library.Context", attributes[2].ConstructorArguments[0].Value);
        Assert.Null(attributes[2].ConstructorArguments[1].Value);
        Assert.Equal("Old.Extensions", attributes[3].ConstructorArguments[1].Value);
        KeyValuePair<string, TypedConstant> extensions = Assert.Single(attributes[3].NamedArguments);
        Assert.Equal("ExtensionMethods", extensions.Key);
        Assert.Equal(["Method(*, string, ...)", "Other"], extensions.Value.Values.Select(value => (string)value.Value!));
    }

    [Fact]
    public void StoresConfigurationWithoutRequiringModelValidation()
    {
        var ignore = new CSharpRewriterIgnoreAttribute("CS0656", "RequiredMemberAttribute");
        Assert.Equal("CS0656", ignore.DiagnosticID);
        Assert.Equal("RequiredMemberAttribute", ignore.MessageContains);
        Assert.Null(new CSharpRewriterIgnoreAttribute("CS0518").MessageContains);

        var mapping = new CSharpRewriterTypeMappingAttribute("Library.Context");
        Assert.Equal("Library.Context", mapping.FullTypeName);
        Assert.Null(mapping.Replaces);
        Assert.Empty(mapping.ExtensionMethods);

        var extensions = new CSharpRewriterTypeMappingAttribute("Library.Extensions", "Old.Extensions")
        {
            ExtensionMethods = ["Method(*)"],
        };
        Assert.Equal("Old.Extensions", extensions.Replaces);
        Assert.Equal(["Method(*)"], extensions.ExtensionMethods);
    }

    [Theory]
    [InlineData(typeof(CSharpRewriterIgnoreAttribute))]
    [InlineData(typeof(CSharpRewriterTypeMappingAttribute))]
    public void AllowsMultipleRulesWithoutInheritance(Type attributeType)
    {
        AttributeUsageAttribute usage = Assert.IsType<AttributeUsageAttribute>(Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute)));
        Assert.True(usage.AllowMultiple);
        Assert.False(usage.Inherited);
        Assert.Equal(AttributeTargets.Assembly, usage.ValidOn);
    }
}
