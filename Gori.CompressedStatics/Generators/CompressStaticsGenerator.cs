using System.Collections.Immutable;
using System.Text;

using Gori.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Extracts symbol-free member models, resolves file content, compresses each member
/// (or a per-type tar blob) once, and emits per-type partial declarations.
/// Symbol access is confined to the attribute transforms.
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

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<CompressionMemberInput> stringMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            Const.CompressStringAttributeFullName,
            static (node, cancellationToken) => node is MethodDeclarationSyntax or PropertyDeclarationSyntax,
            CompressStaticsTransforms.CreateStringMember);

        IncrementalValuesProvider<CompressionMemberInput> fileMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            Const.CompressFileAttributeFullName,
            static (node, cancellationToken) => node is MethodDeclarationSyntax or PropertyDeclarationSyntax,
            CompressStaticsTransforms.CreateFileMember);

        IncrementalValuesProvider<TarMembers> tarMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            Const.TarCompressAttributeFullName,
            static (node, cancellationToken) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
            CompressStaticsTransforms.CreateTarMember);

        IncrementalValueProvider<ImmutableArray<(string Path, string? Text)>> additionalTexts = context.AdditionalTextsProvider
            .Select(static (text, cancellationToken) => (Path.GetFullPath(text.Path), text.GetText(cancellationToken)?.ToString()))
            .Collect();

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

        // Resolve text for CompressFile members
        IncrementalValuesProvider<CompressionMemberInput> resolvedFileMembers = fileMembers
            .Combine(additionalTexts)
            .Combine(supports)
            .Select(static (pair, cancellationToken) => CompressStaticsTransforms.ResolveFileData(pair.Left.Left, pair.Left.Right, pair.Right, cancellationToken));

        // Merge string and file members into a single per-element stream.
        IncrementalValuesProvider<CompressionMemberInput> mergedMembers = stringMembers.Collect()
            .Combine(resolvedFileMembers.Collect())
            .SelectMany(static (pair, cancellationToken) => pair.Left.AddRange(pair.Right));

        // Resolve members for tar-covered members
        IncrementalValuesProvider<(TarMembers Group, Supports Supports)> resolvedTarMembers = tarMembers
            .Combine(mergedMembers.Where(static member => member.ReturnType != ReturnType.Unsupported && member.IsCoveredByTar)
                                  .Collect())
            .Select(static (pair, cancellationToken) => CompressStaticsTransforms.ResolveTarMembers(pair.Left, pair.Right, cancellationToken))
            .Combine(supports);

        // Non-tar members are emitted one file per member. Each member's resolved input is a cached
        // pipeline value, so the source-output callback (where compression happens) only runs for a
        // member whose own input changed; unchanged siblings are never recompressed.
        IncrementalValuesProvider<(CompressionMemberInput Input, Supports Supports)> individualMembers = mergedMembers
            .Where(static member => member.ReturnType != ReturnType.Unsupported && !member.IsCoveredByTar)
            .Combine(supports)
            .WithTrackingName(IndividualMemberTrackingName);

        context.RegisterSourceOutput(mergedMembers, static (productionContext, member) =>
        {
            foreach (DiagnosticInfo diagnostic in member.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });

        context.RegisterSourceOutput(individualMembers, static (productionContext, pair) =>
        {
            string source = MemberEmitter.Emit(productionContext, pair.Input, pair.Supports);
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            productionContext.AddSource(CreateMemberHintName(pair.Input), SourceText.From(source, Encoding.UTF8));
        });

        context.RegisterSourceOutput(resolvedTarMembers, static (productionContext, pair) =>
        {
            productionContext.CancellationToken.ThrowIfCancellationRequested();

            string source = TypeEmitter.EmitTar(productionContext, pair.Group, pair.Supports);
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            productionContext.AddSource(CreateTarHintName(pair.Group.Type), SourceText.From(source, Encoding.UTF8));
        });
    }

    private static string CreateMemberHintName(CompressionMemberInput member)
        => CreateHintName(StripGlobal(member.ContainingType.FullyQualifiedName) + "." + member.MemberName);

    private static string CreateTarHintName(EquatableTypeInfo type)
        => CreateHintName(StripGlobal(type.FullyQualifiedName) + "-Tar");

    private static string StripGlobal(string raw)
        => raw.StartsWith(Const.Global, StringComparison.Ordinal) ? raw[Const.Global.Length..] : raw;

    private static string CreateHintName(string raw)
    {
        var builder = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            _ = builder.Append(char.IsLetterOrDigit(c) || c == '.' || c == '-' ? c : '_');
        }

        return builder.ToString() + "." + Const.HintSuffix + ".g.cs";
    }
}
