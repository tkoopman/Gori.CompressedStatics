// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Generators.Helper.ToStorageKind(Gori.CompressedStatics.CompressionAlgorithm)~Gori.CompressedStatics.Generators.StorageKind")]
[assembly: SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Compressor.Compress(System.Byte[],Gori.CompressedStatics.CompressionAlgorithm@,System.Boolean,System.Threading.CancellationToken)~System.Byte[]")]
[assembly: SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Compressor.CreateCompressionStream(Gori.CompressedStatics.CompressionAlgorithm,System.IO.Stream)~System.IO.Stream")]
[assembly: SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Generators.CompressStaticsTransforms.CreateTarMember(Microsoft.CodeAnalysis.GeneratorAttributeSyntaxContext,System.Threading.CancellationToken)~Gori.CompressedStatics.Generators.TarMembers")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentional")]
[assembly: SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Generators.SourceTemplates.#cctor")]
[assembly: SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Intentional", Scope = "type", Target = "~T:Gori.CompressedStatics.Generators.ReturnType")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.CSharpRewriterTransformer.CreateAttribute(System.String,System.String[])~Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax")]
