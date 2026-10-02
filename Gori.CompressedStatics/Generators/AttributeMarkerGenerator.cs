using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Emits compile-time marker surface used by the compression generators.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AttributeMarkerGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            postInitializationContext.AddEmbeddedAttributeDefinition();
            postInitializationContext.AddSource($"{Const.Namespace}.Attributes.g.cs", EmitAttributeSource());
        });

    private static SourceText EmitAttributeSource()
        => SourceText.From(TransformTemplate(SourceTemplates.AttributeMarkers), Encoding.UTF8);

    private static string TransformTemplate(string text)
    {
        text = text.Replace("public enum", "internal enum");
        text = text.Replace("public sealed class", "internal sealed class");
        return text;
    }
}
