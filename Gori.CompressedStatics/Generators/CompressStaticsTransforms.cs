using System.Collections.Immutable;

using Gori.Roslyn;

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Attribute transforms that build symbol-free pipeline models, plus per-file data resolution.
/// </summary>
internal static class CompressStaticsTransforms
{
    /// <summary>
    /// Transforms a <c>CompressString</c> attribute target into a member model.
    /// </summary>
    /// <param name="context">The attribute context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="CompressionMemberInput"/> representing the member to be generated.</returns>
    public static CompressionMemberInput CreateStringMember(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        AttributeData? attribute = context.Attributes.Length > 0 ? context.Attributes[0] : null;
        string? value = attribute is null
            ? null
            : attribute.GetNamedArgumentAsString(nameof(CompressStringAttribute.Value)) ?? attribute.GetConstructorArgumentAsString();

        cancellationToken.ThrowIfCancellationRequested();

        // The raw literal is retained via SourceValue; its bytes (a UTF-16 encode for a string return
        // type, or a base64 decode for a byte[] return type) are produced on demand at emit time via
        // CompressionMemberInput.GetUncompressedData, which also reports an invalid base64 diagnostic.
        return CreateMember(context, attribute, CompressionSource.String, value, cancellationToken: cancellationToken);
    }

    /// <summary>Transforms a <c>CompressFile</c> attribute target into a member model; bytes are resolved later.</summary>
    /// <param name="context">The attribute context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="CompressionMemberInput"/> representing the member to be generated.</returns>
    public static CompressionMemberInput CreateFileMember(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        AttributeData? attribute = context.Attributes.Length > 0 ? context.Attributes[0] : null;
        string? file = attribute is null
            ? null
            : attribute.GetNamedArgumentAsString(nameof(CompressFileAttribute.File)) ?? attribute.GetConstructorArgumentAsString();

        bool relativeToProject = attribute is not null && attribute.GetNamedArgumentAsBool(nameof(CompressFileAttribute.RelativeToProject), false);

        // SourceValue stays null until the matching additional text is resolved; the normalized path
        // lives in the dedicated Path field. Bytes are produced on demand at emit time.
        return CreateMember(context, attribute, CompressionSource.File, sourceValue: null, path: file, relativeToProject: relativeToProject, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Transforms a <c>TarCompress</c> attribute target into a member model. The members themselves are collected later.
    /// </summary>
    /// <param name="context">The attribute context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="TarMembers"/> representing the member to be generated.</returns>
    public static TarMembers CreateTarMember(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        AttributeData? attribute = context.Attributes.Length > 0 ? context.Attributes[0] : null;
        CompressionAlgorithm algorithm = attribute is null ? CompressionAlgorithm.Auto : attribute.GetNamedArgumentAsEnum(nameof(TarCompressAttribute.Algorithm), CompressionAlgorithm.Auto);
        var location = LocationInfo.From(context.TargetSymbol);

        return context.TargetSymbol is INamedTypeSymbol namedType && namedType.TypeKind is TypeKind.Class or TypeKind.Struct
            ? new TarMembers(
            namedType.CreateEquatableTypeInfo(),
            algorithm,
            location,
            [])
            : throw new InvalidOperationException($"TarCompress attribute can only be applied to a class or struct, but was applied to {context.TargetSymbol.Kind} '{context.TargetSymbol.Name}'");
    }

    /// <summary>
    /// Resolves file member bytes from the collected additional texts. Only matching-path content
    /// affects the result, so unchanged members stay structurally equal and skip re-compression.
    /// </summary>
    /// <param name="member">The member to resolve file data for.</param>
    /// <param name="additionalTexts">The additional texts to search for matching file content.</param>
    /// <param name="supports">The supported features for the current compilation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated <see cref="CompressionMemberInput"/> with resolved file data.</returns>
    public static CompressionMemberInput ResolveFileData(
        CompressionMemberInput member,
        ImmutableArray<(string Path, string? Text)> additionalTexts,
        Supports supports,
        CancellationToken cancellationToken = default)
    {
        string? memberPath = member.GetAbsolutePath(supports.ProjectPath);

        if (member.Source != CompressionSource.File || memberPath is null || member.ReturnType == ReturnType.Unsupported)
        {
            return member;
        }

        bool found = false;
        string? text = null;

        foreach ((string path, string? content) in additionalTexts)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(path, memberPath))
            {
                found = true;
                text = content;
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        if (!found || string.IsNullOrEmpty(text))
        {
            return member;
        }

        // Store the raw additional-file text; its bytes (UTF-16 encode for a string return type, or a
        // base64 decode for a byte[] return type) are produced on demand at emit time via
        // CompressionMemberInput.GetUncompressedData, which also reports an invalid base64 diagnostic.
        return member.WithSourceValue(text);
    }

    /// <summary>
    /// Resolves the members of a <c>TarCompress</c> attribute target from the collected members.
    /// Only matching-type members affect the result, so unchanged members stay structurally equal and skip re-compression.
    /// </summary>
    /// <param name="member">The TarMembers instance to resolve.</param>
    /// <param name="tarMembers">The collection of CompressionMemberInput instances to consider.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A new TarMembers instance with the resolved members.</returns>
    public static TarMembers ResolveTarMembers(
        TarMembers member,
        ImmutableArray<CompressionMemberInput> tarMembers,
        CancellationToken cancellationToken = default)
    {
        if (tarMembers.Length == 0)
        {
            return member;
        }

        var filteredMembers = new List<CompressionMemberInput>();
        foreach (CompressionMemberInput tarMember in tarMembers)
        {
            if (tarMember.ContainingType.Equals(member.Type))
            {
                filteredMembers.Add(tarMember);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return new TarMembers(
            member.Type,
            member.Algorithm,
            member.Location,
            [.. filteredMembers]);
    }

    private static CompressionMemberInput CreateMember(
        GeneratorAttributeSyntaxContext context,
        AttributeData? attribute,
        CompressionSource source,
        string? sourceValue,
        string? path = null,
        bool relativeToProject = false,
        DiagnosticInfo? diagnostic = null,
        CancellationToken cancellationToken = default)
    {
        ISymbol symbol = context.TargetSymbol;
        ReturnType returnType = SymbolExtraction.GetReturnType(symbol);
        var location = LocationInfo.From(symbol);

        var diagnosticList = new List<DiagnosticInfo>();
        if (diagnostic is not null)
        {
            diagnosticList.Add(diagnostic);
        }

        CompressionAlgorithm algorithm = attribute is null ? CompressionAlgorithm.Auto : attribute.GetNamedArgumentAsEnum(nameof(TarCompressAttribute.Algorithm), CompressionAlgorithm.Auto);
        CompressionCacheMode cacheMode = attribute is null ? CompressionCacheMode.OnInit : attribute.GetNamedArgumentAsEnum(nameof(CompressStringAttribute.CacheMode), CompressionCacheMode.OnInit);
        bool isTarCovered = false;

        if (symbol is IPropertySymbol property && AttributeHelper.IsTarCoveredMember(property, cacheMode, out CompressionAlgorithm tarAlgorithm, cancellationToken))
        {
            // If the property is covered by a TarCompress attribute, we override the algorithm to match the tar configuration.
            // This way equality is preserved for the property, even if the property itself has a different algorithm specified,
            // as it is ignored when covered by TAR.
            isTarCovered = true;
            algorithm = tarAlgorithm;
        }

        return new CompressionMemberInput()
        {
            ContainingType = symbol.ContainingType.CreateEquatableTypeInfo(),
            MemberName = symbol.Name,
            MemberKind = SymbolExtraction.GetMemberKind(symbol),
            Accessibility = symbol.DeclaredAccessibility.ToKeywords(),
            IsPartial = symbol.IsPartial(cancellationToken),
            ReturnType = returnType,
            Source = source,
            RequestedAlgorithm = algorithm,
            CacheMode = cacheMode,
            IsCoveredByTar = isTarCovered,
            SourceValue = sourceValue,
            FilePath = path,
            RelativeToProject = relativeToProject,
            Location = location,
            Diagnostics = [.. diagnosticList],
        };
    }
}
