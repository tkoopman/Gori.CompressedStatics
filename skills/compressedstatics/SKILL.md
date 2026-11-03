---
name: gori-compressedstatics
description: Consumer guidance for configuring and using Gori.CompressedStatics attributes, file inputs, compression options, and custom transformers. Use the CSharpRewriter skill for built-in CSharpRewriter options and configuration.
---

# Gori.CompressedStatics

Use this skill when choosing attributes, configuring compression, supplying input files, or implementing a custom input transformer in a project that consumes the package. For the built-in CSharpRewriter, also consult `../csharp-rewriter/SKILL.md`.

## Attributes and inputs

- `CompressString` embeds a string value. `CompressFile` reads a file at compile time. Both can be applied to supported static partial properties or methods returning `string` or `byte[]`.
- `CompressFile` accepts a file included as an `AdditionalFiles` item or a compiled `.cs` file. A matching additional file takes precedence. By default, its path is relative to the source file; `RelativeToProject = true` makes it relative to the project directory.
- To use `RelativeToProject`, expose the MSBuild property to the compiler:

  ```xml
  <ItemGroup>
    <CompilerVisibleProperty Include="MSBuildProjectDirectory" />
  </ItemGroup>
  ```

- `TarCompress` combines eligible properties in a type before compression. Methods and properties excluded by cache mode or the `Exclude` list are handled individually. See the package usage documentation for eligibility details.
- For `byte[]` values, provide Base64 text; it is decoded at compile time. Invalid Base64 is reported during compilation.

## Compression and caching

- `Algorithm` can select Deflate, GZip, Brotli, or Auto. Auto compares available options and may leave the value uncompressed when that is smaller or otherwise more effective.
- Brotli availability depends on the target framework and available compile-time/runtime support. When targeting .NET Standard 2.0, consult the usage documentation for the required Brotli package setup.
- Property cache modes include initialization-time caching and lazy variants. Static methods are not cached. `TarCompress` has additional eligibility restrictions; check the usage documentation before combining it with non-default cache settings.

## Input transformers

- The optional `Transformer` argument transforms input before compression. Built-in `Base64` decoding is selected automatically for byte-array inputs unless another transformer is specified.
- Custom transformers can implement `IStringTransformer` or `IBinaryTransformer` when they only need the input data. Use `IStringCompilationTransformer` or `IBinaryCompilationTransformer` when transformation requires the host compilation.
- Compilation-aware transformers receive the host compilation before this generator adds generated outputs. Additional-file text is not automatically included as syntax in that compilation.
- Keep custom transformations deterministic and thread-safe, honor cancellation, and report actionable failures through `DiagnosticInfo`. Compilation-aware transformations may run again when project source or references change.
- Transformer types and their dependencies must be available to the compiler's analyzer host; they cannot be defined only in the consuming project's source.

## Diagnostics and fixes

- Analyzer diagnostics identify invalid attribute usage and configuration. Fix the source or attribute arguments described by the diagnostic.
- Available code fixes can add required `static` or `partial` modifiers, remove transformer arguments that are incompatible with a selected transformer, and help correct `CompressFile` paths or `RelativeToProject` settings.
- CSharpRewriter-specific diagnostics, assembly configuration, mappings, and fixes are covered in `../csharp-rewriter/SKILL.md`.

For complete attribute signatures, cache behavior, and examples, refer to the `Usage.md` included with the package.
