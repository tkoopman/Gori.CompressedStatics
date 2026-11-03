using System.Collections.Immutable;

using Gori.CompressedStatics.CSharp;

namespace Gori.CompressedStatics.Testing;

internal static class RewriterTestConfiguration
{
    internal static RewriterConfiguration Create(ImmutableArray<TypeMapping> mappings, ImmutableArray<Ignore> ignores = default)
    {
        var configuration = RewriterConfiguration.Create(
            ignores,
            mappings,
            out IEnumerable<RewriteIssue> issues,
            TestContext.Current.CancellationToken);

        Assert.Empty(issues);
        return configuration;
    }
}
