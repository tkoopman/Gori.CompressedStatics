## Attributes

`CompressFile` and `CompressString` can be applied to static methods, and properties that return either a string or byte[].
By default, each attribute is individually compressed. You can add the `TarCompress` attribute 
to a class, to compress all properties in that class together. This however will change available features.
Following table shows where Compress attributes can be applied.

| Apply To Static       | Individual<br>CompressString or CompressFile | TarCompress covered<br>CompressString or CompressFile |
| --------------------- | -------------------------------------------- | ----------------------------------------------------- |
| Auto-properties       | No                                           | Yes ^#1^                                              |
| Partial methods       | Yes                                          | No ^#2^                                               |
| Partial properties    | Yes                                          | Yes                                                   |
| Per-member algorithm  | Yes                                          | No                                                    |
| Per-member cache mode | Yes                                          | No ^#3^                                               |

#1: TarCompress supports auto-properties, as long as no static constructor exists.<br>
#2: Compress attributes on methods are excluded from TarCompress and will always be individually compressed.<br>
#3: Defining `CacheMode` to anything other than `OnInit` will exclude the property from TarCompress.

### Features

### CompressFile Attribute

Will read the contents of a file at compile time and compress it into a static property or method. The file
must be included in the project as an `AdditionalFiles` item, or be a `.cs` file included in the compilation.
Compiled C# source files do not need a separate `AdditionalFiles` entry. The `.cs` extension check is
case-insensitive; file paths use case-sensitive matching, as for additional files.

Additional files take precedence over compiled C# files. C# files are embedded as original source text,
including comments, whitespace, and inactive code. Outputs from other source generators cannot be used as inputs.

Path provided to the attribute is relative to the current file's directory by default. You can add
`RelativeToProject = true` to the attribute to make the path relative to the project directory instead.
For example, this embeds the original text of `Payload.cs` from the same directory:

```csharp
public static partial class EmbeddedSources
{
    [Gori.CompressedStatics.CompressFile("Payload.cs")]
    public static partial string Payload { get; }
}
```

Note: To use RelativeToProject, you must add the following to your project file so that the project directory 
is passed to the compiler.
```xml
<ItemGroup>
    <CompilerVisibleProperty Include="MSBuildProjectDirectory" />
</ItemGroup>
```

#### TarCompress Attribute

You can use the `TarCompress` attribute to combine all properties in a class together before compressing.
This will result in a single compressed value for all properties, which can be more efficient than compressing
each property individually. Decompression will be performed for all tarred properties at once, when the type is 
initialized. If no static constructor exists, one will be created and used to perform the decompression. 
Otherwise, partial properties must be used so the decompression can be added to them when the type is initialized.

To exclude Compress attributes from TarCompress either:
- set the `CacheMode` argument to anything other than `OnInit`, or
- add property name to the `Exclude` argument on `TarCompress`, or
- use Compress attribute on a method.

Excluded compress attributes will follow individual flow as normal.

`TarCompress` changes `CompressFile` and `CompressString` attributes in the same class in the following ways:
- Algorithm not supported. Algorithm defined on `TarCompress` will be used.
- Auto-properties supported, but only if no static constructor exists.

#### Binary Data Support

Both `CompressFile` and `CompressString` support binary values via Base64 encoding.
To use, make the property or method return type `byte[]` instead of `string`. 
The string value will be decoded from Base64 at compile time before compression.

Diagnostic messages will be generated at compile time if the value is not valid Base64.

#### Compression Algorithm

Multiple compression algorithms are supported. By default, at compile time all algorithms will be evaluated
and the best performing algorithm will be selected. This includes leaving the value uncompressed if that is
the best performing option. You can also specify a specific algorithm to use via the `Algorithm` argument.

If the `TarCompress` attribute is used, the best performing algorithm will be selected for the entire class,
and all properties will be compressed with that algorithm. Otherwise, each Compress attribute can use a 
different algorithm.

Empty strings or byte[] values will never be compressed or added to TAR, and will be stored as-is.

NOTE: Brotli algorithm is supported but if you are compiling for .NET Standard 2.0, you will need to add the following
to your project. If not detected at compile time then Brotli will not be used by auto mode.

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="Brotli.NET" Version="2.1.1" />
</ItemGroup>
```

#### Runtime Caching

Depending on where the attribute is applied and if the `TarCompress` attribute is used, the runtime caching behavior will differ. 
The following table shows the caching behavior for each attribute.

| Caching Behavior               | On Method | On Property Individually  | On Property with TarCompress |
| ------------------------------ | --------- | ------------------------- | ---------------------------- |
| No caching                     | Yes       | No                        | No                           |
| Cache on static initialization | No        | Default (OnInit)          | Yes                          |
| Cache on first access          | No        | Optional (Lazy)           | No (Excludes from TAR)       |
| Cache on first access (Lazy<>) | No        | Optional (LazyThreadSafe) | No (Excludes from TAR)       |

To change caching behavior, you can use the `CacheMode` argument on the Compress attributes.

#### String Compression

Note all string values will be stored as Unicode (UTF-16) at compile time. This is to ensure that all
characters are preserved correctly, especially for non-ASCII characters. 

When using CompressFile, the file is read by Roslyn. Roslyn can read files with different encoding as
long as file includes a BOM (Byte Order Mark). UTF-8 is used if no BOM is present.

#### Input Transformers

`CompressFile` and `CompressString` can optionally provide `Transformer` that transforms contents at compile time, before compression or TAR
assembly.

```csharp
[CompressFile("Template.cs",
    Transformer = "CSharpRewriter",
    TransformerArguments = [CSharpRewriterOptions.Default])]
public static partial string Template { get; }
```

There are currently 2 built-in transformers:

| Name             | Description                                                                                                                                                                                                           |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Base64`         | Decodes Base64 input to binary.<br /> This is the only transformer you do not need to reference directly.<br /> It is automatically used if CompressFile is used on a byte array unless another transformer is manually assigned. |
| `CSharpRewriter` | Rewrites C# source for embedding in source generators.                                                                                                                                                                |

**Transformer syntax**

First, the transformer is selected by name. You can also add an optional member name selector that points to a public static field or property.
Member name is added after the transformer type and assembly name, separated by `::`.

| Format                                              | Example                                                            | Description                                                               |
| --------------------------------------------------- | ------------------------------------------------------------------ | ------------------------------------------------------------------------- |
| Full name and assembly                              | `Gori.CompressedStatics.Base64Transformer, Gori.CompressedStatics` | This is the most specific way to reference a transformer.                 |
| Full name and assembly without `Transformer` suffix | `Gori.CompressedStatics.Base64, Gori.CompressedStatics`            | Transformer suffix is optional in all format cases.                       |
| ... + member name                                   | `Gori.CompressedStatics.Base64, Gori.CompressedStatics::Instance`  | Both above options can have member name added after the assembly.         |
| Full name                                           | `Gori.CompressedStatics.Base64Transformer`                         | Assembly is located among assemblies loaded by the generator host.        |
| Short name                                          | `Base64`                                                           | Built-in names take precedence; otherwise loaded assemblies are searched. |
| ... + member name                                   | `Base64::Instance`                                                 | When no assembly, member name added at end.                               |

***NOTE***: Matches outside the built-in transformer namespace must be unambiguous.

**Transformer Arguments**

`TransformerArguments` is an array of values passed to the transformer constructor.
Malformed selectors, null argument arrays, and unavailable argument types are invalid and reported as errors.
Do not combine constructor arguments with a member selector.

**Transformer Load Selection**

The supplied options determine the loading path.
If selected path fails, an error is reported rather than trying another path.
Only the implicit Instance selection can fall back to a parameterless constructor.

1. Member name provided, then public static field or property matching name.
1. Arguments provided, then public constructor matching argument types.<br />
   Arguments must match exact or assignable parameter types and all parameters must be supplied. Params expansion is not performed.
1. Neither provided:
    1. Public static field or property named `Instance` of the same type if exists, else
    1. Public parameterless constructor.

##### C# Rewriter

`CSharpRewriter` prepares C# source for embedding in source generators. Configure it with
`CSharpRewriterOptions`:

| Option                      | Effect                                                                           |
| --------------------------- | -------------------------------------------------------------------------------- |
| `RewritePublicToInternal`   | Change public top-level types to internal; nested accessibility is unchanged.    |
| `AddAutoGeneratedHeader`    | Add an auto-generated source header.                                             |
| `AddAutoGeneratedAttribute` | Add `GeneratedCodeAttribute` using your compilation's assembly name and version. |
| `AddEmbeddedAttribute`      | Add `Microsoft.CodeAnalysis.EmbeddedAttribute` to types.                         |
| `RewriteTypeNames`          | Globally qualify resolvable types and remove unneeded namespace imports.         |
| `RewriteExtensionCalls`     | Convert supported extension invocations into fully qualified static calls.       |
| `AggressiveImportRemoval`   | Remove resolvable imports despite binding errors; requires `RewriteTypeNames`.   |

| Group     | Options                                                                                |
| --------- | -------------------------------------------------------------------------------------- |
| `None`    | No transformations.                                                                    |
| `Basic`   | `RewritePublicToInternal`, `AddAutoGeneratedHeader`, `AddEmbeddedAttribute`.           |
| `Default` | `Basic` plus `AddAutoGeneratedAttribute`, `RewriteTypeNames`, `RewriteExtensionCalls`. |
| `All`     | `Default` plus `AggressiveImportRemoval`.                                              |

Unresolved types and alias references remain unchanged. Aliases, static imports, global imports, and
imports required by extension methods are retained. Global aliases declared in other files must also
be available where the embedded source is used.

Imports are normally retained when the source has binding errors. Use `AggressiveImportRemoval`
with care: it may remove imports needed where the embedded source is used.

XML documentation references such as `<see cref="SomeType" />` are not rewritten, and imports
used only by those references may be removed. Qualify these references yourself or disable
`RewriteTypeNames` to retain the imports.

## Extending Input Transformers

Implement `IStringTransformer` for string output or `IBinaryTransformer` for byte[] output. Both
receive the input as text through `ICompressionAttributeInfo.SourceValue` and return the value to
compress through `TryTransform`. Transformers run at compile time, before individual compression
or TAR assembly.

### Discovery and Construction

Supply the transformer assembly and its dependencies as analyzer dependencies. Transformers cannot
be defined in the consuming project's source. Constructor argument types must also be available to
the generator host.

### Transformation Contract

- Use the supplied `Compilation` to resolve project symbols and references. It excludes generated
  outputs; additional-file text must be parsed and added to a derived compilation for semantic analysis.
- Return transformed output on success. On failure, return false with a `DiagnosticInfo`.
  Messages are reported at the compression attribute. Failed transformations or error diagnostics
  produce an empty value; warnings and informational messages can accompany successful output.
- Keep transformations deterministic and thread-safe. Honor cancellation by throwing
  `OperationCanceledException`.
- Transformations may run again when the compilation changes; do not rely on being called only once.

`DiagnosticInfo` accepts a diagnostic descriptor, message arguments, and an optional severity override.
It does not carry a location; the generator supplies that when reporting.
