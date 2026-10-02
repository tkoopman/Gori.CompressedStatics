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
must be included in the project as an AdditionalFile so that the file will can be read at compile time.

Path provided to the attribute is relative to the current file's directory by default. You can add
`RelativeToProject = true` to the attribute to make the path relative to the project directory instead.

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
