using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Created the Gori.DecompressStatics namespace and members.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DecompressStaticsGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<Supports> supports = context.CompilationProvider.Select(static (compilation, _) => Supports.GetSupported(compilation));

        context.RegisterSourceOutput(supports, static (productionContext, supports) =>
        {
            var builder = CodeStringBuilder.CodeStringBuilder.CreateCSharpBuilder();
            Dictionary<string, string> variables = [];

            variables[Const.DecompressStaticsBrotliVariableName] = supports.BrotliRuntime switch
            {
                BrotliSupport.Legacy => $"{Const.BrotliStreamPrefix}{Const.BrotliStreamName20}{Const.BrotliStreamSuffix}",
                BrotliSupport.Modern => $"{Const.BrotliStreamPrefix}{Const.BrotliStreamName21}{Const.BrotliStreamSuffix}",
                BrotliSupport.None => Const.BrotliStreamNotSupported,
                _ => throw new NotImplementedException(),
            };

            _ = builder.WriteLine(SourceTemplates.DecompressStatics, variables);

            productionContext.AddSource(Const.DecompressStaticsHintName, builder.ToString());
        });
    }
}
