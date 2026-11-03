using Gori.CompressedStatics.CSharp;

namespace Gori.CompressedStatics.Testing;

public sealed class RewriterFactoryTests
{
    public static TheoryData<string?> InvalidMappingDestinations
        => [
            new(null) { Label = "Null destination" },
            new("") { Label = "Empty destination" },
            new("Bad[]") { Label = "Array destination" },
            new("global::Example.Type") { Label = "Alias-qualified destination" },
            new("var") { Label = "Var destination" },
            new("Example.Type<") { Label = "Malformed destination" },
        ];

    [Theory]
    [MemberData(nameof(InvalidMappingDestinations))]
    public void MappingFactoryReturnsIssueWhileConstructorThrows(string? destination)
    {
        Assert.False(TypeMapping.TryCreate(destination, null, out TypeMapping? mapping, out InvalidConfiguration? issue));

        Assert.Null(mapping);
        Assert.NotNull(issue);
        Assert.Equal(InvalidConfigurationReason.InvalidValue, issue.Reason);
        Assert.Equal("fullTypeName", issue.ArgumentName);
        Assert.Equal(destination, issue.ArgumentValue);
        ArgumentException exception = destination is null
            ? Assert.Throws<ArgumentNullException>(() => new TypeMapping(destination!))
            : Assert.Throws<ArgumentException>(() => new TypeMapping(destination));
        Assert.Equal(issue.ArgumentName, exception.ParamName);
        if (destination is not null)
        {
            Assert.StartsWith(issue.Message!, exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MappingFactoryNormalizesNamesAndRetainsExtensions()
    {
        var extension = new ExtensionDefinition("Read(*)");
        Assert.True(TypeMapping.TryCreate(" Example . Context ", " Source . Context ", out TypeMapping? mapping, out InvalidConfiguration? issue, [extension]));

        Assert.Null(issue);
        Assert.Equal("Example.Context", mapping.FullTypeName);
        Assert.Equal("Source.Context", mapping.Replaces);
        Assert.Same(extension, Assert.Single(mapping.ExtensionMethods));
        Assert.Equal(new TypeMapping("Example.Context", "Source.Context") { ExtensionMethods = [extension] }, mapping);

        Assert.True(TypeMapping.TryCreate("Example.Context", null, out mapping, out issue));
        Assert.Null(issue);
        Assert.Equal("Context", mapping.Replaces);
    }

    [Fact]
    public void MappingFactoryIdentifiesInvalidReplacement()
    {
        Assert.False(TypeMapping.TryCreate("Example.Context", "Bad[]", out TypeMapping? mapping, out InvalidConfiguration? issue));
        Assert.Null(mapping);
        Assert.NotNull(issue);
        Assert.Equal("replaces", issue.ArgumentName);
        Assert.Equal("Bad[]", issue.ArgumentValue);
        Assert.Equal("replaces", Assert.Throws<ArgumentException>(() => new TypeMapping("Example.Context", "Bad[]")).ParamName);
    }

    public static TheoryData<string?> InvalidExtensions
        => [
            new(null) { Label = "Null definition" },
            new("") { Label = "Empty definition" },
            new("A.B(*)") { Label = "Qualified method name" },
            new("Read(*") { Label = "Missing closing parenthesis" },
            new("Read(List<string])") { Label = "Mismatched delimiter" },
            new("Read(string>>)") { Label = "Extra generic closing delimiters" },
            new("Read(var)") { Label = "Invalid parameter type" },
            new("Read(*, ..., string)") { Label = "Non-final ellipsis" },
        ];

    [Theory]
    [MemberData(nameof(InvalidExtensions))]
    public void ExtensionFactoryReturnsIssueWhileConstructorThrows(string? definition)
    {
        Assert.False(ExtensionDefinition.TryCreate(definition, out ExtensionDefinition? extension, out InvalidConfiguration? issue));

        Assert.Null(extension);
        Assert.NotNull(issue);
        Assert.Equal(InvalidConfigurationReason.InvalidValue, issue.Reason);
        Assert.Equal("definition", issue.ArgumentName);
        Assert.Equal(definition, issue.ArgumentValue);
        ArgumentException exception = definition is null
            ? Assert.Throws<ArgumentNullException>(() => new ExtensionDefinition(definition!))
            : Assert.Throws<ArgumentException>(() => new ExtensionDefinition(definition));
        Assert.Equal("definition", exception.ParamName);
        if (definition is not null)
        {
            Assert.StartsWith(issue.Message!, exception.Message, StringComparison.Ordinal);
        }
    }

    public static TheoryData<string> ValidExtensions
        => [
            new("Read") { Label = "Method name only" },
            new("Read(*)") { Label = "Wildcard receiver" },
            new("Read(*, ...)") { Label = "Variable arguments" },
            new("Read(System.Collections.Generic.List<System.Collections.Generic.List<string>>, (int, string), int[,])") { Label = "Nested generic tuple and array parameters" },
        ];

    [Theory]
    [MemberData(nameof(ValidExtensions))]
    public void ExtensionFactoryAndConstructorProduceEquivalentValues(string definition)
    {
        Assert.True(ExtensionDefinition.TryCreate(definition, out ExtensionDefinition? extension, out InvalidConfiguration? issue));

        Assert.Null(issue);
        var constructed = new ExtensionDefinition(definition);
        Assert.Equal(constructed, extension);
        Assert.Equal(constructed.Name, extension.Name);
        Assert.Equal(constructed.Parameters.IsDefault, extension.Parameters.IsDefault);
        Assert.Equal(constructed.Parameters.IsDefault ? [] : constructed.Parameters.ToArray(),
            extension.Parameters.IsDefault ? [] : [.. extension.Parameters]);
        Assert.Equal(constructed.AllowExtra, extension.AllowExtra);
    }
}
