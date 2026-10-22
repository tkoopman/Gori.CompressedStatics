namespace Gori.CompressedStatics.Generators;

/// <summary>
/// Carries processing output and its optional diagnostic until their pipeline branches separate.
/// </summary>
/// <typeparam name="T">The processed member or TAR group.</typeparam>
/// <param name="Input">The updated input, or the original when unchanged.</param>
/// <param name="Diagnostic">The diagnostic produced by this stage, if any.</param>
internal readonly record struct ProcessingResult<T>(T Input, DiagnosticInfo? Diagnostic = null);
