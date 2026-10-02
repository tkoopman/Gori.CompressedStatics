#pragma warning disable CS1591, SA1600 // Missing XML comment for publicly visible type or member

using Microsoft.CodeAnalysis;

namespace Gori.CompressedStatics;

/// <summary>
/// Contains diagnostic descriptors for the Gori.CompressedStatics analyzers,
/// providing information about various code issues related to compression attributes.
/// </summary>
public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MemberMustBeStatic = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "001",
        title: "Compress attribute requires static member",
        messageFormat: "Member '{0}' must be declared static when annotated with [{1}]",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberMustBePartial = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "002",
        title: "Compress attribute requires partial member",
        messageFormat: "Member '{0}' must be declared partial when annotated with [{1}]",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OneAttributePerMember = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "003",
        title: "Compress members may only use one compress attribute",
        messageFormat: "Member '{0}' cannot be annotated with both [{1}] and [{2}]",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberMustReturnStringOrByteArray = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "004",
        title: "Compress members must return string or byte[]",
        messageFormat: "Member '{0}' must return 'string' or 'byte[]' when annotated with [{1}]",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodMustHaveNoParameters = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "005",
        title: "Compress methods cannot declare parameters",
        messageFormat: "Method '{0}' must not declare parameters when annotated with [{1}]",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SourceFileMustBeAdditionalFile = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "006",
        title: "Compress source file must be supplied as an additional file",
        messageFormat: "File '{0}' must be provided to the compiler as an additional file",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BrotliNotAvailable = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "007",
        title: "Brotli compression is not supported in this compilation",
        messageFormat: "Brotli compression is not supported in this compilation.{0}",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.Compiler]);

    public static readonly DiagnosticDescriptor IgnoreCacheModeOnMethods = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "008",
        title: "CacheMode is ignored for methods",
        messageFormat: "Method '{0}' ignores CacheMode because methods always decompress per call",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.Unnecessary]);

    public static readonly DiagnosticDescriptor CSharpLanguageVersionMustBe13OrGreater = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "009",
        title: "C# language version must be 13.0 or newer",
        messageFormat: "C# language version must be 13.0 or newer. Current language version is '{0}'.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor TypeMustBePartial = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "010",
        title: "TarCompress's type must be partial",
        messageFormat: "Containing type '{0}' must be declared partial when [{1}] attribute is used",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MSBuildProjectDirectoryNotVisible = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "011",
        title: $"Using {nameof(CompressFileAttribute.RelativeToProject)} requires MSBuildProjectDirectory property to be made visible to compiler",
        messageFormat: $"Using {nameof(CompressFileAttribute.RelativeToProject)} requires MSBuildProjectDirectory property to be made visible to compiler. Add <CompilerVisibleProperty Include=\"MSBuildProjectDirectory\" /> to your project file.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IgnoreAlgorithmOnTarMember = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "012",
        title: "Algorithm is ignored for TarCompress members",
        messageFormat: "Member '{0}' is covered by TarCompress, so its Algorithm argument is ignored",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.Unnecessary]);

    public static readonly DiagnosticDescriptor InvalidBase64String = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "013",
        title: "Invalid Base64 string",
        messageFormat: "Member '{0}' contains an invalid Base64 string. When return type is byte[] string must be a valid Base64 encoded string.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.Compiler]);

    public static readonly DiagnosticDescriptor MemberMustBePartialWhenTarHasStaticConstructor = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "014",
        title: "TarCompress members must be partial when a static constructor exists",
        messageFormat: "Member '{0}' must be declared partial because its containing type '{1}' declares a static constructor",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericTypesUnsupported = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "015",
        title: "Compress attributes should not be added to generic types",
        messageFormat: "[{0}] should not be used in generic types",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
