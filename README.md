# Gori.CompressedStatics

This library provides a way to compress static values in C#, and decompress them during runtime.
It is useful for reducing the size of static data in applications.

Main features:
- No runtime dependencies.
- Compress selected static values to reduce compiled size.
- Values can be supplied either from a string or additional text file(s).
- Binary data supported by using byte[], and supplying value using Base64 encoded, string or text file(s).
    - Base64 decoding will be done at compile time, so no runtime overhead.
- Applied to static properties, decompressed value is cached.
    - Cache options for OnInit, Lazy, and thread safe lazy initialization.
- Applied to static methods, decompressed value is not cached.
- When multiple OnInit properties are used, can be configured to Tar the values before compression to reduce size further.
- Multiple algorithms are supported for compression. (Decompress, GZip and Brotli (if available))
- Default algorithm is Auto, which tries all available algorithms at compile time and uses the most effective one.
    - Will leave uncompressed if that is the most effective option.
- Code Fixes for:
    - Add missing partial modifier
    - Add missing static modifier
    - Removing of ignored Compress attribute parameters, when TarCompress used
    - Incorrect relative file path with option to change RelativeToProject or File's path.
      This is shown when file doesn't exist as additional file, but does if RelativeToProject is inverted.

## Usage

Please see [Usage.md](Usage.md) for detailed usage instructions and examples.
