using System.Collections.Immutable;

using Gori.Common.Roslyn;

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
    /// <returns>A <see cref="CompressAttributeInfo"/> representing the member to be generated.</returns>
    public static CompressAttributeInfo CreateStringMember(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Attributes.Length == 0)
        {
            return CompressAttributeInfo.Null;
        }

        AttributeData attribute = context.Attributes[0];

        string? value = attribute.GetNamedArgumentAsString(nameof(CompressStringAttribute.Value)) ?? attribute.GetConstructorArgumentAsString();

        cancellationToken.ThrowIfCancellationRequested();

        return CreateMember(context, attribute, CompressAttributeType.CompressString, value, cancellationToken: cancellationToken);
    }

    /// <summary>Transforms a <c>CompressFile</c> attribute target into a member model; bytes are resolved later.</summary>
    /// <param name="context">The attribute context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="CompressAttributeInfo"/> representing the member to be generated.</returns>
    public static CompressAttributeInfo CreateFileMember(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Attributes.Length == 0)
        {
            return CompressAttributeInfo.Null;
        }

        AttributeData attribute = context.Attributes[0];

        string? file = attribute.GetNamedArgumentAsString(nameof(CompressFileAttribute.File)) ?? attribute.GetConstructorArgumentAsString();
        bool relativeToProject = attribute.GetNamedArgumentAsBool(nameof(CompressFileAttribute.RelativeToProject), false);

        // SourceValue stays null until the matching file text is resolved; the path
        // lives in the dedicated FilePath field. Bytes are produced on demand at emit time.
        return CreateMember(
            context,
            attribute,
            CompressAttributeType.CompressFile,
            sourceValue: null,
            path: file,
            relativeToProject: relativeToProject,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Transforms a <c>TarCompress</c> attribute target into a member model. The members themselves are collected later.
    /// </summary>
    /// <param name="context">The attribute context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="TarMembers"/> representing the member to be generated.</returns>
    public static TarMembers CreateTarMember(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Attributes.Length == 0)
        {
            return TarMembers.Null;
        }

        AttributeData attribute = context.Attributes[0];

        CompressionAlgorithm algorithm = attribute.GetNamedArgumentAsEnum(nameof(TarCompressAttribute.Algorithm), CompressionAlgorithm.Auto);
        var location = LocationInfo.From(attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation());

        return context.TargetSymbol is INamedTypeSymbol namedType && namedType.TypeKind is TypeKind.Class or TypeKind.Struct
            ? new TarMembers(
            namedType.CreateEquatableTypeInfo(),
            algorithm,
            location,
            [])
            : throw new InvalidOperationException($"TarCompress attribute can only be applied to a class or struct, but was applied to {context.TargetSymbol.Kind} '{context.TargetSymbol.Name}'");
    }

    /// <summary>
    /// Resolves file member text from additional texts, falling back to compiled C# source files.
    /// Only matching-path content affects the result, so unchanged members skip re-compression.
    /// </summary>
    /// <param name="member">The member to resolve file data for.</param>
    /// <param name="additionalTexts">The additional texts to search for matching file content.</param>
    /// <param name="supports">The supported features for the current compilation.</param>
    /// <param name="compilation">The compilation to search for C# source files.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated <see cref="CompressAttributeInfo"/> with resolved file data.</returns>
    public static CompressAttributeInfo ResolveFileData(
        CompressAttributeInfo member,
        ImmutableArray<AdditionalText> additionalTexts,
        Supports supports,
        Compilation compilation,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!member.IsValid || member.AttributeType != CompressAttributeType.CompressFile)
        {
            return member;
        }

        string? memberPath;
        try
        {
            memberPath = member.GetAbsolutePath(supports.ProjectPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // The file analyzer reports invalid paths; generation still supplies an empty implementation.
            return member.Invalidate();
        }

        if (memberPath is null)
        {
            return member;
        }

        bool found = false;
        string? text = null;

        foreach (AdditionalText additionalText in additionalTexts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (StringComparer.Ordinal.Equals(Path.GetFullPath(additionalText.Path), memberPath))
            {
                found = true;
                text = additionalText.GetText(cancellationToken)?.ToString();
                break;
            }
        }

        if (!found)
        {
            text = SourceFileHelper.FindSyntaxTree(compilation, memberPath, cancellationToken)?.GetText(cancellationToken).ToString();
        }

        return text is null ? member : member.WithSourceValue(text);
    }

    /// <summary>
    /// Resolves the members of a <c>TarCompress</c> attribute target from the collected members.
    /// Only matching-type members affect the result, so unchanged members stay structurally equal and skip re-compression.
    /// </summary>
    /// <param name="member">The TarMembers instance to resolve.</param>
    /// <param name="tarMembers">The collection of <see cref="CompressAttributeInfo"/> instances to include.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A new TarMembers instance with the resolved members.</returns>
    public static TarMembers ResolveTarMembers(
        TarMembers member,
        ImmutableArray<CompressAttributeInfo> tarMembers,
        CancellationToken cancellationToken = default)
    {
        if (!member.IsValid || tarMembers.Length == 0)
        {
            return member;
        }

        var filteredMembers = new List<CompressAttributeInfo>();
        foreach (CompressAttributeInfo tarMember in tarMembers)
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

    /// <summary>Validates the compression support required by an individual member.</summary>
    /// <param name="member">The transformed member.</param>
    /// <param name="supports">Available compression support.</param>
    /// <returns>The member and any compression-support diagnostic.</returns>
    public static ProcessingResult<CompressAttributeInfo> ValidateCompression(CompressAttributeInfo member, Supports supports)
        => member.IsValid && supports.DisableBrotli && member.RequestedAlgorithm is CompressionAlgorithm.Brotli
            ? new(member.Invalidate(), CreateBrotliDiagnostic(supports))
            : new(member);

    /// <summary>Validates the compression support required by a TAR group.</summary>
    /// <param name="group">The resolved group.</param>
    /// <param name="supports">Available compression support.</param>
    /// <returns>The group and any compression-support diagnostic.</returns>
    public static ProcessingResult<TarMembers> ValidateCompression(TarMembers group, Supports supports)
        => group.IsValid && group.Members.Length != 0 && supports.DisableBrotli && group.Algorithm is CompressionAlgorithm.Brotli
            ? new(group.Invalidate(), CreateBrotliDiagnostic(supports))
            : new(group);

    private static DiagnosticInfo CreateBrotliDiagnostic(Supports supports)
    {
        string missing = supports.BrotliCompileTime ? "Missing runtime assembly."
            : supports.BrotliRuntime is not BrotliSupport.None ? "Missing compile time assembly."
            : "Missing compile time and runtime assemblies.";
        return new DiagnosticInfo(DiagnosticDescriptors.BrotliNotAvailable, missing);
    }

    private static CompressAttributeInfo CreateMember(
        GeneratorAttributeSyntaxContext context,
        AttributeData attribute,
        CompressAttributeType attributeType,
        string? sourceValue,
        string? path = null,
        bool relativeToProject = false,
        CancellationToken cancellationToken = default)
    {
        ISymbol symbol = context.TargetSymbol;
        ReturnType returnType = SymbolExtraction.GetReturnType(symbol);
        var location = LocationInfo.From(attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation());

        CompressionAlgorithm algorithm = attribute.GetNamedArgumentAsEnum(nameof(TarCompressAttribute.Algorithm), CompressionAlgorithm.Auto);
        CompressionCacheMode cacheMode = attribute.GetNamedArgumentAsEnum(nameof(CompressStringAttribute.CacheMode), CompressionCacheMode.OnInit);
        bool isTarCovered = false;

        if (symbol is IPropertySymbol property && AttributeHelper.IsTarCoveredMember(property, cacheMode, out CompressionAlgorithm tarAlgorithm, cancellationToken))
        {
            // If the property is covered by a TarCompress attribute, we override the algorithm to match the tar configuration.
            // This way equality is preserved for the property, even if the property itself has a different algorithm specified,
            // as it is ignored when covered by TAR.
            isTarCovered = true;
            algorithm = tarAlgorithm;
        }

        string? transformerSelector = attribute.GetNamedArgumentAsString(nameof(CompressFileAttribute.Transformer));
        string? transformerType = null;
        string? transformerMember = null;
        ImmutableArray<TransformerArgument> transformerArguments = default;

        bool validAttribute = returnType is not ReturnType.Unsupported
            && ValueTransformerHelper.IsValidTransformerValue(transformerSelector, out transformerType, out transformerMember);

        if (transformerType is not null
            && transformerMember is null
            && attribute.TryGetNamedArgument(nameof(CompressFileAttribute.TransformerArguments), out TypedConstant argument))
        {
            if (argument.Kind != TypedConstantKind.Array || argument.IsNull)
            {
                transformerType = null;
                validAttribute = false;
                transformerArguments = [];
            }
            else
            {
                transformerArguments = [.. argument.Values.Select(TransformerArgument.FromConstant)];
            }
        }

        var member = new CompressAttributeInfo()
        {
            ContainingType = symbol.ContainingType.CreateEquatableTypeInfo(),
            MemberName = symbol.Name,
            MemberKind = SymbolExtraction.GetMemberKind(symbol),
            Accessibility = symbol.DeclaredAccessibility.ToKeywords(),
            IsPartial = symbol.IsPartial(cancellationToken),
            ReturnType = returnType,
            AttributeType = attributeType,
            RequestedAlgorithm = algorithm,
            CacheMode = cacheMode,
            IsCoveredByTar = isTarCovered,
            SourceValue = sourceValue,
            FilePath = path,
            RelativeToProject = relativeToProject,
            Transformer = transformerType,
            TransformerMember = transformerMember,
            TransformerArguments = transformerArguments,
            Location = location,
        };

        return validAttribute ? member : member.Invalidate();
    }
}
