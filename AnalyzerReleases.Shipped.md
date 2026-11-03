; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2.1.0

### New Rules

Rule ID | Category               | Severity | Notes
--------|------------------------|----------|--------------------
GCS034  | CSharpRewriter         | Error    | Invalid rewriter configuration
GCS035  | CSharpRewriter         | Warning  | Unfiltered diagnostic ignore rule
GCS036  | CSharpRewriter         | Error    | Incomplete type mapping placeholder
GCS037  | CSharpRewriter         | Error    | Conflicting configuration at the same scope
GCS038  | CSharpRewriter         | Warning  | Compiler binding error in rewritten source
GCS039  | CSharpRewriter         | Warning  | Unresolvable reference in rewritten source
GCS040  | CSharpRewriter         | Warning  | Mapping overlaps an ignored compiler diagnostic

## Release 1.0.1

### New Rules

Rule ID | Category               | Severity | Notes
--------|------------------------|----------|--------------------
GCS016  | Gori.CompressedStatics | Error    | InvalidTransformerFormat
GCS017  | Gori.CompressedStatics | Error    | TransformerTypeNotFound
GCS018  | Gori.CompressedStatics | Error    | TransformerTypeNotConcreteClass
GCS019  | Gori.CompressedStatics | Error    | TransformerIsAmbiguous
GCS020  | Gori.CompressedStatics | Error    | TransformerTypeIsGeneric
GCS021  | Gori.CompressedStatics | Error    | TransformerTypeNotAssignable
GCS022  | Gori.CompressedStatics | Error    | TransformerTypeHasNoPublicParameterlessConstructor
GCS023  | Gori.CompressedStatics | Error    | TransformerMemberNotFound
GCS024  | Gori.CompressedStatics | Error    | TransformerMemberNotAssignable
GCS025  | Gori.CompressedStatics | Error    | TransformerExceptionThrownLoadingInstance
GCS026  | Gori.CompressedStatics | Error    | TransformerUnknownErrorLoadingInstance
GCS027  | Gori.CompressedStatics | Error    | TransformerExceptionThrown
GCS028  | Gori.CompressedStatics | Error    | InvalidCSharpTransformerSource
GCS029  | Gori.CompressedStatics | Error    | TransformerArgumentsWithMember
GCS030  | Gori.CompressedStatics | Error    | TransformerConstructorNotFound
GCS031  | Gori.CompressedStatics | Error    | TransformerConstructorIsAmbiguous
GCS032  | Gori.CompressedStatics | Error    | InvalidTransformerArgument
GCS033  | Gori.CompressedStatics | Error    | TransformerArgumentsRequireType

## Release 1.0

### New Rules

Rule ID | Category               | Severity | Notes
--------|------------------------|----------|--------------------
GCS001  | Gori.CompressedStatics | Error    | MemberMustBeStatic
GCS002  | Gori.CompressedStatics | Error    | MemberMustBePartial
GCS003  | Gori.CompressedStatics | Error    | OneAttributePerMember
GCS004  | Gori.CompressedStatics | Error    | MemberMustReturnStringOrByteArray
GCS005  | Gori.CompressedStatics | Error    | MethodMustHaveNoParameters
GCS006  | Gori.CompressedStatics | Error    | SourceFileMustBeAdditionalFile
GCS007  | Gori.CompressedStatics | Error    | BrotliNotAvailable
GCS008  | Gori.CompressedStatics | Warning  | IgnoreCacheModeOnMethods
GCS009  | Gori.CompressedStatics | Error    | CSharpLanguageVersionMustBe13OrGreater
GCS010  | Gori.CompressedStatics | Error    | TypeMustBePartial
GCS011  | Gori.CompressedStatics | Error    | MSBuildProjectDirectoryNotVisible
GCS012  | Gori.CompressedStatics | Warning  | IgnoreAlgorithmOnTarMember
GCS013  | Gori.CompressedStatics | Error    | InvalidBase64String
GCS014  | Gori.CompressedStatics | Error    | MemberMustBePartialWhenTarHasStaticConstructor
GCS015  | Gori.CompressedStatics | Warning  | GenericTypesUnsupported
