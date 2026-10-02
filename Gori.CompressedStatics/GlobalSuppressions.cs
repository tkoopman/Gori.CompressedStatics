// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Generators.Helper.ToStorageKind(Gori.CompressedStatics.CompressionAlgorithm)~Gori.CompressedStatics.Generators.StorageKind")]
[assembly: SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Compressor.Compress(System.Byte[],Gori.CompressedStatics.CompressionAlgorithm@,System.Boolean,System.Threading.CancellationToken)~System.Byte[]")]
[assembly: SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Compressor.CreateCompressionStream(Gori.CompressedStatics.CompressionAlgorithm,System.IO.Stream)~System.IO.Stream")]
[assembly: SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Generators.MemberEmitter.Emit(Microsoft.CodeAnalysis.SourceProductionContext,Gori.CompressedStatics.Generators.CompressionMemberInput,Gori.CompressedStatics.Generators.Supports)~System.String")]
[assembly: SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Generators.CompressStaticsTransforms.CreateTarMember(Microsoft.CodeAnalysis.GeneratorAttributeSyntaxContext,System.Threading.CancellationToken)~Gori.CompressedStatics.Generators.TarMembers")]
[assembly: SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Generators.TypeEmitter.AppendTarLazy(Gori.CodeStringBuilder.CodeStringBuilder,Gori.CompressedStatics.Generators.TarMembers,System.Byte[],Gori.CompressedStatics.Generators.StorageKind,System.Collections.Generic.List{System.ValueTuple{Gori.CompressedStatics.Generators.CompressionMemberInput,System.Int32,System.Int32}})")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentional")]
[assembly: SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Intentional", Scope = "member", Target = "~M:Gori.CompressedStatics.Generators.SourceTemplates.#cctor")]
