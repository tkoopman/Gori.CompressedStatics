; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

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
