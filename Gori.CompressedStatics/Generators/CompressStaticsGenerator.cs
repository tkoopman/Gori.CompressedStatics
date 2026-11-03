using System.Collections.Immutable;
using System.Text;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Extracts symbol-free member models, resolves file content, compresses each member
/// (or a per-type tar blob) once, and emits per-type partial declarations.
/// Compilation access is confined to input transforms and transformation, before compression and emission.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CompressStaticsGenerator : IIncrementalGenerator
{
    /// <summary>
    /// The tracking name for the per-member individual emission input step, exposed so tests can
    /// assert that unchanged non-tar members are not re-emitted (and thus not recompressed) when a
    /// sibling member changes.
    /// </summary>
    internal const string IndividualMemberTrackingName = "IndividualMember";

    /// <summary>The tracking name for location-independent TAR emission inputs.</summary>
    internal const string TarTrackingName = "TarEmission";

    /// <summary>The tracking name for transformation results before diagnostics branch off.</summary>
    internal const string TransformationTrackingName = "Transformation";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<CompressAttributeInfo> stringMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            Const.CompressStringAttributeFullName,
            static (node, cancellationToken) => node is MethodDeclarationSyntax or PropertyDeclarationSyntax,
            CompressStaticsTransforms.CreateStringMember)
            .Where(static member => !member.IsNull);

        IncrementalValuesProvider<CompressAttributeInfo> fileMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            Const.CompressFileAttributeFullName,
            static (node, cancellationToken) => node is MethodDeclarationSyntax or PropertyDeclarationSyntax,
            CompressStaticsTransforms.CreateFileMember)
            .Where(static member => !member.IsNull);

        IncrementalValuesProvider<TarMembers> tarMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            Const.TarCompressAttributeFullName,
            static (node, cancellationToken) => node is TypeDeclarationSyntax,
            CompressStaticsTransforms.CreateTarMember)
            .Where(static member => !member.IsNull);

        IncrementalValueProvider<ImmutableArray<AdditionalText>> additionalTexts = context.AdditionalTextsProvider.Collect();

        IncrementalValueProvider<string?> projectDirProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, cancellationToken) =>
            {
                _ = provider.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out string? projectDirectory);
                return projectDirectory;
            });

        IncrementalValueProvider<Supports> supports = context.CompilationProvider
            .Select(static (compilation, cancellationToken) => Supports.GetSupported(compilation))
            .Combine(projectDirProvider)
            .Select(static (pair, cancellationToken) => pair.Left.WithProjectPath(pair.Right ?? string.Empty));

        IncrementalValuesProvider<(string? SourceValue, bool IsValid)> resolvedFileContents = fileMembers
            .WithComparer(CompressAttributeInfo.FilePathComparer)
            .Combine(additionalTexts)
            .Combine(supports)
            .Combine(context.CompilationProvider)
            .Select(static (pair, cancellationToken) =>
            {
                CompressAttributeInfo resolved = CompressStaticsTransforms.ResolveFileData(pair.Left.Left.Left, pair.Left.Left.Right, pair.Left.Right, pair.Right, cancellationToken);
                return (resolved.SourceValue, resolved.IsValid);
            });

        // Both collections preserve fileMembers order. Reattach content and validity, never cached locations.
        IncrementalValuesProvider<CompressAttributeInfo> resolvedFileMembers = fileMembers.Collect()
            .Combine(resolvedFileContents.Collect())
            .SelectMany(static (pair, cancellationToken) => pair.Left.Select((member, index) => pair.Right[index].IsValid
                ? member.WithSourceValue(pair.Right[index].SourceValue)
                : member.Invalidate()));

        IncrementalValuesProvider<CompressAttributeInfo> allMembers = stringMembers.Collect()
            .Combine(resolvedFileMembers.Collect())
            .SelectMany(static (pair, cancellationToken) => pair.Left.AddRange(pair.Right));

        IncrementalValuesProvider<CompressAttributeInfo> noTransformerMembers = allMembers
            .Where(static member => !member.TransformerType.IsValid(noneIsValid: false));

        IncrementalValuesProvider<CompressAttributeInfo> basicTransformerMembers = allMembers
            .Where(static member => member.TransformerType is TransformerType.Binary or TransformerType.String);

        IncrementalValuesProvider<CompressAttributeInfo> compilationTransformerMembers = allMembers
            .Where(static member => member.TransformerType is TransformerType.BinaryCompilation or TransformerType.StringCompilation);

        IncrementalValuesProvider<ProcessingResult<CompressAttributeInfo>> basicTransformationResults = basicTransformerMembers
            .Select(static (pair, cancellationToken) => pair.TransformInput(null, cancellationToken))
            .WithTrackingName(TransformationTrackingName);

        context.RegisterSourceOutput(basicTransformationResults, static (productionContext, result) =>
        {
            foreach (DiagnosticInfo diagnostic in result.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic.ToDiagnostic(result.Input.Location.ToLocation()));
            }
        });

        IncrementalValuesProvider<ProcessingResult<CompressAttributeInfo>> compilationTransformationResults = compilationTransformerMembers
            .Combine(context.CompilationProvider)
            .Select(static (pair, cancellationToken) => pair.Left.TransformInput(pair.Right, cancellationToken))
            .WithTrackingName(TransformationTrackingName);

        context.RegisterSourceOutput(compilationTransformationResults, static (productionContext, result) =>
        {
            foreach (DiagnosticInfo diagnostic in result.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic.ToDiagnostic(result.Input.Location.ToLocation()));
            }
        });

        IncrementalValuesProvider<CompressAttributeInfo> mergedMembers = noTransformerMembers.Collect()
            .Combine(basicTransformationResults.Select(static (result, _) => result.Input).Collect())
            .Combine(compilationTransformationResults.Select(static (result, _) => result.Input).Collect())
            .SelectMany(static (tuple, _) =>
            {
                ((ImmutableArray<CompressAttributeInfo> arr1, ImmutableArray<CompressAttributeInfo> arr2), ImmutableArray<CompressAttributeInfo> arr3) = tuple;
                return arr1.Concat(arr2).Concat(arr3);
            });

        // Resolve members for tar-covered members
        IncrementalValuesProvider<ProcessingResult<TarMembers>> tarResults = tarMembers
            .Combine(mergedMembers.Where(static member => member.ReturnType is not ReturnType.Unsupported && member.IsCoveredByTar)
                                  .Collect())
            .Select(static (pair, cancellationToken) => CompressStaticsTransforms.ResolveTarMembers(pair.Left, pair.Right, cancellationToken))
            .Combine(supports)
            .Select(static (pair, cancellationToken) => CompressStaticsTransforms.ValidateCompression(pair.Left, pair.Right));

        context.RegisterSourceOutput(tarResults, static (productionContext, result) =>
        {
            foreach (DiagnosticInfo diagnostic in result.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic.ToDiagnostic(result.Input.Location.ToLocation()));
            }
        });

        // Non-tar members are emitted one file per member. Each member's resolved input is a cached
        // pipeline value, so the source-output callback (where compression happens) only runs for a
        // member whose own input changed; unchanged siblings are never recompressed.
        IncrementalValuesProvider<ProcessingResult<CompressAttributeInfo>> individualResults = mergedMembers
            .Where(static member => member.ReturnType is not ReturnType.Unsupported && !member.IsCoveredByTar)
            .Combine(supports)
            .Select(static (pair, cancellationToken) => CompressStaticsTransforms.ValidateCompression(pair.Left, pair.Right));

        context.RegisterSourceOutput(individualResults, static (productionContext, result) =>
        {
            foreach (DiagnosticInfo diagnostic in result.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic.ToDiagnostic(result.Input.Location.ToLocation()));
            }
        });

        IncrementalValuesProvider<(CompressAttributeInfo Input, Supports Supports)> individualMembers = individualResults
            .Select(static (result, cancellationToken) => result.Input)
            .WithComparer(CompressAttributeInfo.EmissionComparer)
            .Combine(supports)
            .WithTrackingName(IndividualMemberTrackingName);

        context.RegisterSourceOutput(individualMembers, static (productionContext, pair) =>
        {
            string source = MemberEmitter.Emit(productionContext, pair.Input, pair.Supports);
            if (source.IsWhiteSpace())
            {
                return;
            }

            productionContext.AddSource(CreateMemberHintName(pair.Input), SourceText.From(source, Encoding.UTF8));
        });

        IncrementalValuesProvider<(TarMembers Left, Supports Right)> tarEmission = tarResults
            .Select(static (result, cancellationToken) => result.Input)
            .WithComparer(TarMembers.EmissionComparer)
            .Combine(supports)
            .WithTrackingName(TarTrackingName);

        context.RegisterSourceOutput(tarEmission, static (productionContext, pair) =>
        {
            productionContext.CancellationToken.ThrowIfCancellationRequested();

            string source = TypeEmitter.EmitTar(productionContext, pair.Left, pair.Right);
            if (source.IsWhiteSpace())
            {
                return;
            }

            productionContext.AddSource(CreateTarHintName(pair.Left.Type), SourceText.From(source, Encoding.UTF8));
        });
    }

    private static string CreateMemberHintName(CompressAttributeInfo member)
        => CreateHintName(StripGlobal(member.ContainingType.FullyQualifiedName) + "." + member.MemberName);

    private static string CreateTarHintName(EquatableTypeInfo type)
        => CreateHintName(StripGlobal(type.FullyQualifiedName) + "-Tar");

    private static string StripGlobal(string raw)
        => raw.StartsWith(Const.Global, StringComparison.Ordinal) ? raw[Const.Global.Length..] : raw;

    private static string CreateHintName(string raw)
        => Helper.Sanitize(raw, preserveSeparators: true) + "." + Const.HintSuffix + ".g.cs";
}
