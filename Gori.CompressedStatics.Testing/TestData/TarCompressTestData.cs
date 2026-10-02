using System.Collections;

namespace Gori.CompressedStatics.Testing.TestData;

/// <summary>
/// Provides test data for the <see cref="CompressSourceGeneratorTests"/> tests.
///
/// TheoryDataRow contains
/// <list type="number">
/// <item><term>source</term><description>The source code to analyze.</description></item>
/// <item><term>expected</term><description>An array of strings that should be found in the resulting source from the generator.</description></item>
/// <item><term>not expected</term><description>An array of strings that should not be found in the resulting source from the generator.</description></item>
/// </list>
/// </summary>
internal sealed class TarCompressTestData : IEnumerable<TheoryDataRow<string, string[], string[]>>
{
    public IEnumerator<TheoryDataRow<string, string[], string[]>> GetEnumerator()
    {
        yield return new TheoryDataRow<string, string[], string[]>(
            """
            using Gori.CompressedStatics;

            [TarCompress(Algorithm = CompressionAlgorithm.GZip)]
            public static partial class Sample
            {
                [CompressString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
                public static partial string ValueA { get; }

                [CompressString("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")]
                public static partial string ValueB { get; }
            }
            """,
            [
                $"private static readonly byte[] {Const.TarCompressedFieldName}",
                $"byte[] {Const.DecompressedTarFieldName} =",
                $"{Const.DecompressStaticsClassFullName}.SegmentAsString({Const.DecompressedTarFieldName}",
                "static Sample()",
            ],
            [
                Const.TarBufferMethodName,
            ])
        {
            Label = "TarCompress: Uses static constructor",
        };

        yield return new TheoryDataRow<string, string[], string[]>(
            """
            using Gori.CompressedStatics;
            
            [TarCompress]
            public static partial class Sample
            {
                static Sample()
                {
                }
            
                [CompressString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
                public static partial string Value { get; }
            }
            """,
            [
                $"private static byte[] {Const.TarBufferMethodName}",
                "global::System.Threading.LazyInitializer.EnsureInitialized",
                $"{Const.CacheFieldNamePrefix}Value = global::",
            ],
            [
                "static Sample()",
            ])
        {
            Label = "TarCompress: User static constructor existed",
        };

        yield return new TheoryDataRow<string, string[], string[]>(
            """
            using Gori.CompressedStatics;
            
            [TarCompress(Algorithm = CompressionAlgorithm.GZip)]
            public static partial class Sample
            {
                [CompressString("")]
                public static partial string EmptyValue { get; }
            }
            """,
            [
                Const.CacheFieldNamePrefix,
                "= \"\";",
            ],
            [
                Const.TarCompressedFieldName,
                Const.TarBufferMethodName,
            ])
        {
            Label = "TarCompress: Direct assignment when value is empty",
        };

        yield return new TheoryDataRow<string, string[], string[]>(
            """
            using Gori.CompressedStatics;
            
            [TarCompress]
            public static partial class Sample
            {
                [CompressString("a")]
                public static partial string A { get; }
            
                [CompressString("b")]
                public static partial string B { get; }
            }
            """,
            [
                "= \"a\";",
                "= \"b\";",
            ],
            [
                Const.TarCompressedFieldName,
                Const.TarBufferMethodName,
            ])
        {
            Label = "TarCompress: Direct assignment when value left uncompressed",
        };

        yield return new TheoryDataRow<string, string[], string[]>(
            """
            using Gori.CompressedStatics;
            
            [TarCompress(Algorithm = CompressionAlgorithm.GZip, Exclude = new[] { "Excluded" })]
            public static partial class Sample
            {
                [CompressString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
                public static partial string Included { get; }
            
                [CompressString("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", Algorithm = CompressionAlgorithm.GZip)]
                public static partial string Excluded { get; }
            }
            """,
            [
                $"private static readonly byte[] {Const.TarCompressedFieldName} = [",
                $"private static readonly byte[] {Const.CompressedFieldNamePrefix}Excluded = [",
                $"private static readonly string {Const.CacheFieldNamePrefix}Excluded = global::",
                $"public static partial string Excluded => {Const.CacheFieldNamePrefix}Excluded;",
                $"private static string {Const.CacheFieldNamePrefix}Included;",
                $"public static partial string Included => {Const.CacheFieldNamePrefix}Included;",
            ],
            [
                Const.TarBufferMethodName,
            ])
        {
            Label = "TarCompress: Excluded by name",
        };

        yield return new TheoryDataRow<string, string[], string[]>(
            """
            using Gori.CompressedStatics;
            
            [TarCompress(Algorithm = CompressionAlgorithm.GZip)]
            public static partial class Sample
            {
                [CompressString("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", CacheMode = CompressionCacheMode.OnInit)]
                public static partial string Included { get; }
            
                [CompressString("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", CacheMode = CompressionCacheMode.Lazy)]
                public static partial string Excluded { get; }
            }
            """,
            [
                $"private static readonly byte[] {Const.TarCompressedFieldName} = [",
                $"private static readonly byte[] {Const.CompressedFieldNamePrefix}Excluded = [",
                $"private static string {Const.CacheFieldNamePrefix}Excluded;",
                $"public static partial string Excluded\r\n",
                $"  => {Const.CacheFieldNamePrefix}Excluded ??= global::",
                $"private static string {Const.CacheFieldNamePrefix}Included;",
                $"public static partial string Included => {Const.CacheFieldNamePrefix}Included;",
            ],
            [
                Const.TarBufferMethodName,
            ])
        {
            Label = "TarCompress: Excluded by custom cache mode",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
