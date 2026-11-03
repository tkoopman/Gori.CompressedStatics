using System.Text;

using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Emits compile-time marker surface used by the compression generators.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AttributesGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            var rewriter = new CSharpRewriter();
            CSharpCompilation compilation = CreateAttributeCompilation();

            postInitializationContext.AddEmbeddedAttributeDefinition();
            postInitializationContext.AddSource(
                $"{Const.Namespace}.CompressionAlgorithm.g.cs",
                EmitAttributeSource(rewriter, compilation, SourceTemplates.CompressionAlgorithm, postInitializationContext.CancellationToken));

            postInitializationContext.AddSource(
                $"{Const.Namespace}.CompressionCacheMode.g.cs",
                EmitAttributeSource(rewriter, compilation, SourceTemplates.CompressionCacheMode, postInitializationContext.CancellationToken));

            postInitializationContext.AddSource(
                $"{Const.Namespace}.CompressStringAttribute.g.cs",
                EmitAttributeSource(rewriter, compilation, SourceTemplates.CompressStringAttribute, postInitializationContext.CancellationToken));

            postInitializationContext.AddSource(
                $"{Const.Namespace}.CompressFileAttribute.g.cs",
                EmitAttributeSource(rewriter, compilation, SourceTemplates.CompressFileAttribute, postInitializationContext.CancellationToken));

            postInitializationContext.AddSource(
                $"{Const.Namespace}.TarCompressAttribute.g.cs",
                EmitAttributeSource(rewriter, compilation, SourceTemplates.TarCompressAttribute, postInitializationContext.CancellationToken));

            postInitializationContext.AddSource(
                $"{Const.Namespace}.CSharpRewriterIgnoreAttribute.g.cs",
                EmitAttributeSource(rewriter, compilation, SourceTemplates.CSharpRewriterIgnoreAttribute, postInitializationContext.CancellationToken));

            postInitializationContext.AddSource(
                $"{Const.Namespace}.CSharpRewriterOptions.g.cs",
                EmitAttributeSource(rewriter, compilation, SourceTemplates.CSharpRewriterOptions, postInitializationContext.CancellationToken));

            postInitializationContext.AddSource(
                $"{Const.Namespace}.CSharpRewriterTypeMappingAttribute.g.cs",
                EmitAttributeSource(rewriter, compilation, SourceTemplates.CSharpRewriterTypeMappingAttribute, postInitializationContext.CancellationToken));
        });

    private static SourceText EmitAttributeSource(CSharpRewriter rewriter, CSharpCompilation compilation, string source, CancellationToken cancellationToken)
    {
        string rewrittenSource = rewriter.Rewrite(
            source,
            compilation,
            out IEnumerable<RewriteIssue> issues,
            cancellationToken);

        return issues.Any()
            ? throw new InvalidOperationException(
                $"Unexpected C# rewriter issues while generating marker source: {string.Join(", ", issues.Select(issue => issue.GetType().Name))}")
            : SourceText.From(rewrittenSource, Encoding.UTF8);
    }

    private static CSharpCompilation CreateAttributeCompilation()
    {
        string[] sources =
        [
            SourceTemplates.CompressionAlgorithm,
            SourceTemplates.CompressionCacheMode,
            SourceTemplates.CompressStringAttribute,
            SourceTemplates.CompressFileAttribute,
            SourceTemplates.TarCompressAttribute,
            SourceTemplates.CSharpRewriterIgnoreAttribute,
            SourceTemplates.CSharpRewriterOptions,
            SourceTemplates.CSharpRewriterTypeMappingAttribute,
        ];

        SyntaxTree[] syntaxTrees = [.. sources.Select(source => CSharpSyntaxTree.ParseText(source))];
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
        ];

        return CSharpCompilation.Create("Gori.CompressedStatics.AttributeTemplates", syntaxTrees, references);
    }
}
