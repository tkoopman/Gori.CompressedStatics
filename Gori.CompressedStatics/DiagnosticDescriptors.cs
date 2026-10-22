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
        title: "Compress source file must be available to the compiler",
        messageFormat: "File '{0}' must be provided to the compiler as an additional file or a compiled C# source file (.cs)",
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

    public static readonly DiagnosticDescriptor InvalidTransformerFormat = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "016",
        title: "Invalid transformer selector format",
        messageFormat: "Member '{0}' has an invalid Transformer selector. Specify a type name optionally followed by '::MemberName'.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerTypeNotFound = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "017",
        title: "Transformer type was not found",
        messageFormat: "Member '{0}' specifies a Transformer type that could not be found",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerTypeNotConcreteClass = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "018",
        title: "Transformer type must be a concrete class",
        messageFormat: "Member '{0}' specifies a Transformer type that must be a non-abstract class when no static member is selected",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerIsAmbiguous = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "019",
        title: "Transformer selector is ambiguous",
        messageFormat: "Member '{0}' has a Transformer selector that matches multiple types or members. Specify an assembly-qualified type name and an unambiguous static member.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerTypeIsGeneric = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "020",
        title: "Transformer type contains unbound generic parameters",
        messageFormat: "Member '{0}' specifies a Transformer type with unbound generic parameters. Specify a closed type.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerTypeNotAssignable = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "021",
        title: "Transformer type must implement the required interface",
        messageFormat: "Member '{0}' specifies a Transformer type that must implement IStringTransformer for string results or IBinaryTransformer for byte[] results",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerTypeHasNoPublicParameterlessConstructor = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "022",
        title: "Transformer type cannot be instantiated",
        messageFormat: "Member '{0}' specifies a Transformer type with neither a usable public static Instance member of the same type nor a public parameterless constructor",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerMemberNotFound = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "023",
        title: "Transformer static member was not found",
        messageFormat: "Member '{0}' specifies a Transformer member that could not be found as a public static field or property with a public static getter",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerMemberNotAssignable = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "024",
        title: "Transformer static member is incompatible",
        messageFormat: "Member '{0}' specifies an incompatible Transformer member. Use a non-indexed public static field or property whose declared type implements IStringTransformer for string results or IBinaryTransformer for byte[] results.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerExceptionThrownLoadingInstance = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "025",
        title: "An exception occurred while loading the transformer",
        messageFormat: "Member '{0}' could not load its transformer because type resolution, member access, or instance construction threw an exception",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerUnknownErrorLoadingInstance = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "026",
        title: "Transformer instance could not be loaded",
        messageFormat: "Member '{0}' could not load its transformer. Ensure the selected constructor or static member supplies a non-null transformer instance.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerExceptionThrown = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "027",
        title: "An exception occurred while transforming the input",
        messageFormat: "Member '{0}' could not transform its value using transformer '{1}': {2}",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidCSharpTransformerSource = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "028",
        title: "Invalid C# source in transformer input",
        messageFormat: "Member '{0}' has invalid C# source in its transformer input: {1}",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerArgumentsWithMember = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "029",
        title: "Transformer arguments cannot select a static member",
        messageFormat: "Member '{0}' specifies TransformerArguments together with a ::MemberName selector. Select a transformer type without a static member.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerConstructorNotFound = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "030",
        title: "No matching public transformer constructor",
        messageFormat: "Member '{0}' has no public Transformer constructor matching its explicit argument types. Only exact or assignable types are supported.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerConstructorIsAmbiguous = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "031",
        title: "Transformer constructor is ambiguous",
        messageFormat: "Member '{0}' has explicit Transformer arguments matching multiple public constructors without a unique most-specific match",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidTransformerArgument = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "032",
        title: "Invalid transformer constructor argument",
        messageFormat: "Member '{0}' has invalid TransformerArguments. Use a non-null array of attribute constants whose enum, typeof, and array types can be loaded.",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TransformerArgumentsRequireType = new DiagnosticDescriptor(
        id: Const.DiagnosticDescriptorIDPrefix + "033",
        title: "Transformer arguments require a transformer type",
        messageFormat: "Member '{0}' specifies TransformerArguments without a Transformer type",
        category: Const.DiagnosticDescriptorCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
