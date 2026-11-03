using Gori.CompressedStatics.CSharp;

namespace Gori.CompressedStatics.Testing;

public sealed class ExtensionDefinitionTests
{
    public static TheoryData<string, string[], bool> ValidDefinitions
        => [
            new("Method", [], true) { Label = "Name only" },
            new("Method(*)", ["*"], false) { Label = "Receiver only" },
            new("Method(*, string)", ["*", "string"], false) { Label = "Simple argument" },
            new("Method(*, Dictionary<string, int>)", ["*", "Dictionary<string,int>"], false) { Label = "Generic comma" },
            new("Method(*, Dictionary<string, List<Tuple<int, string>>>)", ["*", "Dictionary<string,List<Tuple<int,string>>>"], false) { Label = "Nested generic closing tokens" },
            new("Method(*, int[,], string[,,][])", ["*", "int[,]", "string[,,][]"], false) { Label = "Array rank commas" },
            new("Method(*, (string, int))", ["*", "(string,int)"], false) { Label = "Tuple parentheses" },
            new("Method(*, (string Name, int Count))", ["*", "(string,int)"], false) { Label = "Tuple element names ignored" },
            new("Method(*, List<(int Count, (string Name, int Size) Details)[]>)", ["*", "List<(int,(string,int))[]>"], false) { Label = "Nested tuple element names ignored" },
            new("Method(*, Dictionary<string, (int, string[])>, ...)", ["*", "Dictionary<string,(int,string[])>"], true) { Label = "Nested types and extra arguments" },
            new("Method(...)", [], true) { Label = "Only extra arguments" },
            new("Method(* /* , < ( [ */, string)", ["*", "string"], false) { Label = "Comment punctuation after wildcard" },
            new("Method(*, Library /* , < ( [ */ . @class)", ["*", "Library.@class"], false) { Label = "Comments and escaped identifiers" },
            new(" Method ( * , int[,] )  ", ["*", "int[,]"], false) { Label = "Surrounding whitespace" },
        ];

    [Theory]
    [MemberData(nameof(ValidDefinitions))]
    public void ParsesParameterBoundaries(string text, string[] expectedParameters, bool expectedAllowExtra)
    {
        var definition = new ExtensionDefinition(text);
        Assert.Equal("Method", definition.Name);
        Assert.Equal(expectedParameters, definition.Parameters.IsDefault ? [] : [.. definition.Parameters]);
        Assert.Equal(expectedAllowExtra, definition.AllowExtra);
    }

    public static TheoryData<string> InvalidDefinitions
        => [
            new("") { Label = "Empty definition" },
            new("Method()") { Label = "Empty receiver" },
            new("Method(*") { Label = "Missing closing parenthesis" },
            new("Method(*) trailing") { Label = "Trailing tokens" },
            new("Method(*))") { Label = "Extra closing parenthesis" },
            new("Method(*, List<int)") { Label = "Unclosed generic" },
            new("Method(*, int[)") { Label = "Unclosed array" },
            new("Method(*, (int, string)") { Label = "Unclosed tuple" },
            new("Method(*, List<int])") { Label = "Mismatched delimiters" },
            new("Method(*, List<int>>)") { Label = "Extra generic closer" },
            new("Method(*,,int)") { Label = "Empty parameter" },
            new("Method(*,)") { Label = "Trailing comma" },
            new("Method(..., int)") { Label = "Extra marker not final" },
            new("Method(*, ..., ...)") { Label = "Repeated extra marker" },
            new("Method<T>(*)") { Label = "Generic method name prohibited" },
        ];

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void RejectsMalformedDefinitions(string text)
    {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ExtensionDefinition(text));
        Assert.Equal("definition", error.ParamName);
    }

    [Fact]
    public void RejectsNullDefinition()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new ExtensionDefinition(null!));
        Assert.Equal("definition", error.ParamName);
    }
}
