using System.Collections;

namespace Gori.CompressedStatics.Testing.TestData;

/// <summary>
/// Provides test data for the <see cref="AnalyzerTests.RemoveIgnoredArgumentCodeFixProviderTest"/> tests.
/// </summary>
internal sealed class IgnoredArgumentsAnalyzerTestData : IEnumerable<TheoryDataRow<MockSourceData>>
{
    private const string Category = "Analyzer + CodeFix: Ignored Arguments";

    public IEnumerator<TheoryDataRow<MockSourceData>> GetEnumerator()
    {
        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
            
                public static partial class Sample
                {
                    [CompressFile("data.txt", Algorithm = CompressionAlgorithm.GZip, CacheMode = CompressionCacheMode.LazyThreadSafe)]
                    public static partial string Data { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for CompressFile with Algorithm and CacheMode on Property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
            
                public static partial class Sample
                {
                    [CompressString("hello", Algorithm = CompressionAlgorithm.GZip, CacheMode = CompressionCacheMode.LazyThreadSafe)]
                    public static partial string Value { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for CompressString with Algorithm and CacheMode on Property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
            
                public static partial class Sample
                {
                    [CompressString("hello", Algorithm = CompressionAlgorithm.GZip)]
                    public static partial string GetText();
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for CompressString with Algorithm on Method",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                [TarCompress]
                public static partial class Sample
                {
                    [CompressString("hello", Algorithm = CompressionAlgorithm.GZip, CacheMode = CompressionCacheMode.LazyThreadSafe)]
                    public static partial string Value { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for TarCompress + CompressString with Algorithm and CacheMode on Property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                [TarCompress(Exclude = [nameof(Sample.Value)])]
                public static partial class Sample
                {
                    [CompressString("hello", Algorithm = CompressionAlgorithm.GZip)]
                    public static partial string Value { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for TarCompress (excluding property) + CompressString with and Algorithm on Property",
        };

        // Methods excluded from TarCompress, so no diagnostics should be raised for the following case.
        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                [TarCompress]
                public static partial class Sample
                {
                    [CompressFile("data.txt", Algorithm = CompressionAlgorithm.GZip)]
                    public static partial string GetData();
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for TarCompress + CompressFile with Algorithm on Method",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                [TarCompress]
                public static partial class Sample
                {
                    [CompressFile("data.txt", Algorithm = CompressionAlgorithm.GZip)]
                    public static partial string Data { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.IgnoreAlgorithmOnTarMember.Id,
                DiagnosticMessageExpected =
                [
                    "'Data'",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    """[CompressFile("data.txt")]"""
                ],
                CodeFixNotExpected =
                [
                    "Algorithm",
                    "CompressionAlgorithm.GZip"
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise warning for TarCompress + CompressFile with Algorithm on Property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                [TarCompress]
                public static partial class Sample
                {
                    [CompressString("hello", Algorithm = CompressionAlgorithm.GZip)]
                    public static partial string Value { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.IgnoreAlgorithmOnTarMember.Id,
                DiagnosticMessageExpected =
                [
                    "'Value'",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    """[CompressString("hello")]"""
                ],
                CodeFixNotExpected =
                [
                    "Algorithm",
                    "CompressionAlgorithm.GZip"
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise warning for TarCompress + CompressString with Algorithm on Property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                [TarCompress(Exclude = [nameof(Sample.Value)])]
                public static partial class Sample
                {
                    [CompressString("hello1", Algorithm = CompressionAlgorithm.GZip)]
                    public static partial string Value { get; }

                    [CompressString("hello2", Algorithm = CompressionAlgorithm.GZip)]
                    public static partial string Value2 { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.IgnoreAlgorithmOnTarMember.Id,
                DiagnosticMessageExpected =
                [
                    "'Value2'",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    """[CompressString("hello1", Algorithm = CompressionAlgorithm.GZip)]""",
                    """[CompressString("hello2")]"""
                ],
                CodeFixNotExpected =
                [
                    """[CompressString("hello1")]""",
                    """[CompressString("hello2", Algorithm = CompressionAlgorithm.GZip)]"""
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise warning for TarCompress (exclude unrelated) + CompressString with Algorithm on Property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                public static partial class Sample
                {
                    [CompressString("hello", CacheMode = CompressionCacheMode.LazyThreadSafe)]
                    public static partial string GetText();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.IgnoreCacheModeOnMethods.Id,
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "[CompressString(\"hello\")]",
                ],
                CodeFixNotExpected =
                [
                    "CacheMode",
                    "LazyThreadSafe",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Removes CacheMode argument from CompressString on method",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                public static partial class Sample
                {
                    [CompressFile("data.txt", CacheMode = CompressionCacheMode.OnInit)]
                    public static partial string GetData();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.IgnoreCacheModeOnMethods.Id,
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "[CompressFile(\"data.txt\")]",
                ],
                CodeFixNotExpected =
                [
                    "CacheMode",
                    "OnInit",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Removes CacheMode argument from CompressFile on method",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;

                public static partial class Sample
                {
                    [CompressString("hello", CacheMode = CompressionCacheMode.LazyThreadSafe, Algorithm = CompressionAlgorithm.GZip)]
                    public static partial string GetText();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.IgnoreCacheModeOnMethods.Id,
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "[CompressString(\"hello\", Algorithm = CompressionAlgorithm.GZip)]",
                ],
                CodeFixNotExpected =
                [
                    "CacheMode",
                    "LazyThreadSafe",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Removes only CacheMode argument, keeps other arguments",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
