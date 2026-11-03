using Gori.CompressedStatics.CSharp;

namespace Gori.CompressedStatics.Testing;

public class TypeMappingClassTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("System.String", null, "String")]
    [InlineData("System.Int32", "Int32", null)]
    [InlineData("MyNamespace.UnknownTypeName", "UnknownTypeName", null)]
    [InlineData(" MyNamespace.UnknownTypeName ", "UnknownTypeName", null)]
    [InlineData("UnknownTypeName", null, "UnknownTypeName")]
    [InlineData("@class", null, "@class")]
    public void TypeMapping_ValidTypeNames_ShouldSucceed(string fullTypeName, string? replaces, string? expectedReplaces)
    {
        // Act
        var mapping = new TypeMapping(fullTypeName, replaces);

        output.WriteLine($"Created TypeMapping: FullTypeName='{mapping.FullTypeName}', Replaces='{mapping.Replaces}'");

        // Assert
        Assert.Equal(fullTypeName.Trim(), mapping.FullTypeName);
        if (replaces is not null)
        {
            Assert.Equal(replaces.Trim(), mapping.Replaces);
        }

        if (expectedReplaces is not null)
        {
            Assert.Equal(expectedReplaces, mapping.Replaces);
        }
    }

    [Theory]
    [InlineData("", "Name cannot be null, empty or whitespace.")]
    [InlineData(" ", "Name cannot be null, empty or whitespace.")]
    [InlineData("global::MyNamespace.UnknownTypeName", "aliases (including global::)")]
    [InlineData("MyNamespace.Unknown Type Name", "Unexpected token")]
    [InlineData("MyNamespace.UnknownTypeName.", "Identifier expected")]
    [InlineData("UnknownTypeName.", "Identifier expected")]
    [InlineData(".UnknownTypeName", "Type expected")]
    [InlineData("class", "Type expected")]
    [InlineData("System.Collections.Generic.List<System.String>", "without generic arguments")]
    public void TypeMapping_InvalidTypeNames_ShouldFail(string fullTypeName, string expectedMessage)
    {
        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new TypeMapping(fullTypeName));
        output.WriteLine($"Caught expected exception: {exception.Message}");
        Assert.Contains(expectedMessage, exception.Message);
    }
}
