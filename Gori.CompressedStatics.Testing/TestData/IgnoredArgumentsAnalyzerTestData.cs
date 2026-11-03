using System.Collections;

using Gori.Common.Roslyn.Test;

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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.IgnoreAlgorithmOnTarMember)
                    {
                        IsExpected = true,
                        Message = "'Data'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = """[CompressFile("data.txt")]""" },
                            new CodeFixExpectation { IsExpected = false, Code = "Algorithm" },
                            new CodeFixExpectation { IsExpected = false, Code = "CompressionAlgorithm.GZip" },
                        ],
                    },
                    DiagnosticExpectation.NoOthers,
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.IgnoreAlgorithmOnTarMember)
                    {
                        IsExpected = true,
                        Message = "'Value'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = """[CompressString("hello")]""" },
                            new CodeFixExpectation { IsExpected = false, Code = "Algorithm" },
                            new CodeFixExpectation { IsExpected = false, Code = "CompressionAlgorithm.GZip" },
                        ],
                    },
                    DiagnosticExpectation.NoOthers,
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.IgnoreAlgorithmOnTarMember)
                    {
                        IsExpected = true,
                        Message = "'Value2'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = """[CompressString("hello1", Algorithm = CompressionAlgorithm.GZip)]""" },
                            new CodeFixExpectation { IsExpected = true, Code = """[CompressString("hello2")]""" },
                            new CodeFixExpectation { IsExpected = false, Code = """[CompressString("hello1")]""" },
                            new CodeFixExpectation { IsExpected = false, Code = """[CompressString("hello2", Algorithm = CompressionAlgorithm.GZip)]""" },
                        ],
                    },
                    DiagnosticExpectation.NoOthers,
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.IgnoreCacheModeOnMethods)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "[CompressString(\"hello\")]" },
                            new CodeFixExpectation { IsExpected = false, Code = "CacheMode" },
                            new CodeFixExpectation { IsExpected = false, Code = "LazyThreadSafe" },
                        ],
                    },
                    DiagnosticExpectation.NoOthers,
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.IgnoreCacheModeOnMethods)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "[CompressFile(\"data.txt\")]" },
                            new CodeFixExpectation { IsExpected = false, Code = "CacheMode" },
                            new CodeFixExpectation { IsExpected = false, Code = "OnInit" },
                        ],
                    },
                    DiagnosticExpectation.NoOthers,
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.IgnoreCacheModeOnMethods)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "[CompressString(\"hello\", Algorithm = CompressionAlgorithm.GZip)]" },
                            new CodeFixExpectation { IsExpected = false, Code = "CacheMode" },
                            new CodeFixExpectation { IsExpected = false, Code = "LazyThreadSafe" },
                        ],
                    },
                    DiagnosticExpectation.NoOthers,
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Removes only CacheMode argument, keeps other arguments",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
