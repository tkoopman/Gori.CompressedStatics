namespace Gori.CompressedStatics.Testing;

public sealed class TransformerSelectorTests
{
    public static TheoryData<string> RewriterSelectors
        => [
            new("CSharpRewriter") { Label = "Short shorthand" },
            new("CSharpRewriterTransformer") { Label = "Short exact name" },
            new("Gori.CompressedStatics.CSharpRewriter") { Label = "Qualified shorthand" },
            new("Gori.CompressedStatics.CSharpRewriterTransformer") { Label = "Qualified exact name" },
            new("Gori.CompressedStatics.CSharpRewriter, Gori.CompressedStatics") { Label = "Assembly-qualified shorthand" },
            new(typeof(CSharpRewriterTransformer).AssemblyQualifiedName!) { Label = "Assembly-qualified exact name" },
        ];

    [Theory]
    [MemberData(nameof(RewriterSelectors))]
    public void RewriterCanBeSelectedWithOrWithoutSuffix(string selector)
    {
        Assert.Equal(Generators.TransformerType.StringCompilation, ValueTransformerHelper.IsValidTransformerValue(selector, Generators.ReturnType.String, out string? type, out string? member, out ValueTransformerHelper.Error error));
        Assert.NotNull(type);
        Assert.Null(member);
        Assert.Equal(ValueTransformerHelper.Error.None, error);
    }

    public static TheoryData<string> Base64Selectors
        => [
            new("Base64") { Label = "Short shorthand wins over unrelated Base64" },
            new("Base64Transformer") { Label = "Short full name" },
            new("Gori.CompressedStatics.Base64") { Label = "Qualified shorthand" },
            new("Base64::Instance") { Label = "Short shorthand with member" },
        ];

    [Theory]
    [MemberData(nameof(Base64Selectors))]
    public void Base64ShorthandStillPrefersSingleton(string selector)
    {
        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, Generators.ReturnType.ByteArray, out string? type, out string? member, out _).IsValid());
        Assert.NotNull(type);
        Assert.True(ValueTransformerHelper.TryGetTransformer(type, member, out IBinaryTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.Same(Base64Transformer.Instance, transformer);
    }

    [Fact]
    public void ExactNameTakesPrecedenceOverSuffixedName()
    {
        Assert.True(ValueTransformerHelper.TryGetTransformer(nameof(ExactSelector), null, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        _ = Assert.IsType<ExactSelector>(transformer);
    }

    [Fact]
    public void ShorthandCanSelectStaticMember()
    {
        string typeName = typeof(SelectorProviderTransformer).FullName!;
        string selector = $"{typeName[..^"Transformer".Length]}, {typeof(SelectorProviderTransformer).Assembly.GetName().Name}::Selected";
        Assert.Equal(Generators.TransformerType.StringCompilation, ValueTransformerHelper.IsValidTransformerValue(selector, Generators.ReturnType.String, out string? type, out string? member, out ValueTransformerHelper.Error error));
        Assert.NotNull(type);
        Assert.Equal(nameof(SelectorProviderTransformer.Selected), member);
        Assert.Equal(ValueTransformerHelper.Error.None, error);
    }

    [Fact]
    public void AmbiguousShorthandIsRejected()
    {
        Assert.Equal(Generators.TransformerType.NotFound, ValueTransformerHelper.IsValidTransformerValue("AmbiguousSelector", Generators.ReturnType.String, out _, out _, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.IsAmbiguous, error);
    }

    public sealed class Base64;

    public sealed class ExactSelector : TypeHelperTests.StringTransformer;

    public sealed class ExactSelectorTransformer : TypeHelperTests.StringTransformer;

    public static class SelectorProviderTransformer
    {
        public static readonly IStringCompilationTransformer Selected = new CSharpRewriterTransformer();
    }

    public static class FirstProvider
    {
        public sealed class AmbiguousSelectorTransformer : CSharpRewriterTransformer;
    }

    public static class SecondProvider
    {
        public sealed class AmbiguousSelectorTransformer : CSharpRewriterTransformer;
    }
}
