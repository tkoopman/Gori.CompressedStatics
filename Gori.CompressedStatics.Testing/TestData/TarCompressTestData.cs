using System.Collections;

using Gori.Common.Roslyn.Test;

namespace Gori.CompressedStatics.Testing.TestData;

/// <summary>
/// Provides test data for the <see cref="CompressSourceGeneratorTests"/> tests.
/// </summary>
internal sealed class TarCompressTestData : IEnumerable<TheoryDataRow<MockSourceData>>
{
    public IEnumerator<TheoryDataRow<MockSourceData>> GetEnumerator()
    {
        yield return new(new MockSourceData
        {
            SourceCode =
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
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = $"private static readonly byte[] {Const.TarCompressedFieldName}" },
                new FileExpectation { IsExpected = true, Content = $"byte[] {Const.DecompressedTarFieldName} =" },
                new FileExpectation { IsExpected = true, Content = $"{Const.DecompressStaticsClassFullName}.SegmentAsString({Const.DecompressedTarFieldName}" },
                new FileExpectation { IsExpected = true, Content = "static Sample()" },
                new FileExpectation { IsExpected = false, Content = Const.TarBufferMethodName },
            ],
        })
        {
            Label = "TarCompress: Uses static constructor",
        };

        yield return new(new MockSourceData
        {
            SourceCode =
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
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = $"private static byte[] {Const.TarBufferMethodName}" },
                new FileExpectation { IsExpected = true, Content = "global::System.Threading.LazyInitializer.EnsureInitialized" },
                new FileExpectation { IsExpected = true, Content = $"{Const.CacheFieldNamePrefix}Value = global::" },
                new FileExpectation { IsExpected = false, Content = "static Sample()" },
            ],
        })
        {
            Label = "TarCompress: User static constructor existed",
        };

        yield return new(new MockSourceData
        {
            SourceCode =
            """
            using Gori.CompressedStatics;

            [TarCompress(Algorithm = CompressionAlgorithm.GZip)]
            public static partial class Sample
            {
                [CompressString("")]
                public static partial string EmptyValue { get; }
            }
            """,
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = Const.CacheFieldNamePrefix },
                new FileExpectation { IsExpected = true, Content = "= \"\";" },
                new FileExpectation { IsExpected = false, Content = Const.TarCompressedFieldName },
                new FileExpectation { IsExpected = false, Content = Const.TarBufferMethodName },
            ],
        })
        {
            Label = "TarCompress: Direct assignment when value is empty",
        };

        yield return new(new MockSourceData
        {
            SourceCode =
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
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = "= \"a\";" },
                new FileExpectation { IsExpected = true, Content = "= \"b\";" },
                new FileExpectation { IsExpected = false, Content = Const.TarCompressedFieldName },
                new FileExpectation { IsExpected = false, Content = Const.TarBufferMethodName },
            ],
        })
        {
            Label = "TarCompress: Direct assignment when value left uncompressed",
        };

        yield return new(new MockSourceData
        {
            SourceCode =
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
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = $"private static readonly byte[] {Const.TarCompressedFieldName} = [" },
                new FileExpectation { IsExpected = true, Content = $"private static readonly byte[] {Const.CompressedFieldNamePrefix}Excluded = [" },
                new FileExpectation { IsExpected = true, Content = $"private static readonly string {Const.CacheFieldNamePrefix}Excluded = global::" },
                new FileExpectation { IsExpected = true, Content = $"public static partial string Excluded => {Const.CacheFieldNamePrefix}Excluded;" },
                new FileExpectation { IsExpected = true, Content = $"private static string {Const.CacheFieldNamePrefix}Included;" },
                new FileExpectation { IsExpected = true, Content = $"public static partial string Included => {Const.CacheFieldNamePrefix}Included;" },
                new FileExpectation { IsExpected = false, Content = Const.TarBufferMethodName },
            ],
        })
        {
            Label = "TarCompress: Excluded by name",
        };

        yield return new(new MockSourceData
        {
            SourceCode =
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
            Expectations =
            [
                new FileExpectation { IsExpected = true, Content = $"private static readonly byte[] {Const.TarCompressedFieldName} = [" },
                new FileExpectation { IsExpected = true, Content = $"private static readonly byte[] {Const.CompressedFieldNamePrefix}Excluded = [" },
                new FileExpectation { IsExpected = true, Content = $"private static string {Const.CacheFieldNamePrefix}Excluded;" },
                new FileExpectation { IsExpected = true, Content = $"public static partial string Excluded\r\n" },
                new FileExpectation { IsExpected = true, Content = $"  => {Const.CacheFieldNamePrefix}Excluded ??= global::" },
                new FileExpectation { IsExpected = true, Content = $"private static string {Const.CacheFieldNamePrefix}Included;" },
                new FileExpectation { IsExpected = true, Content = $"public static partial string Included => {Const.CacheFieldNamePrefix}Included;" },
                new FileExpectation { IsExpected = false, Content = Const.TarBufferMethodName },
            ],
        })
        {
            Label = "TarCompress: Excluded by custom cache mode",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
