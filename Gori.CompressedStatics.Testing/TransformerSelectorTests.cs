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
        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.True(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        _ = Assert.IsType<CSharpRewriterTransformer>(transformer);
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
        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
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
        Assert.True(ValueTransformerHelper.IsValidTransformerValue(selector, out string? type, out string? member));
        Assert.NotNull(type);
        Assert.True(ValueTransformerHelper.TryGetTransformer(type, member, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.None, error);
        Assert.Same(SelectorProviderTransformer.Selected, transformer);
    }

    [Fact]
    public void AmbiguousShorthandIsRejected()
    {
        Assert.False(ValueTransformerHelper.TryGetTransformer("AmbiguousSelector", null, out IStringTransformer? transformer, out ValueTransformerHelper.Error error));
        Assert.Equal(ValueTransformerHelper.Error.IsAmbiguous, error);
        Assert.Null(transformer);
    }

    public sealed class Base64;

    public sealed class ExactSelector : CSharpRewriterTransformer;

    public sealed class ExactSelectorTransformer : CSharpRewriterTransformer;

    public static class SelectorProviderTransformer
    {
        public static readonly IStringTransformer Selected = new CSharpRewriterTransformer();
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
