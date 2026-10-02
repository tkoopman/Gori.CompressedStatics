using System.Collections;

namespace Gori.CompressedStatics.Testing.TestData;

/// <summary>
/// Provides test data for the <see cref="AnalyzerTests.CompressAttributeAnalyzerTest"/> tests.
/// </summary>
internal sealed class GenericTypeAnalyzerTestData : IEnumerable<TheoryDataRow<MockSourceData>>
{
    private const string Category = "Analyzer: Generic Type Tests";

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
                    [CompressFile("hello.txt")]
                    public static partial string Value { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for CompressFile on a non-generic class",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressString("hello")]
                    public static partial string Value { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for CompressString on a non-generic class",
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
                    [CompressString("hello")]
                    public static partial string Value { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for TarCompress on a non-generic class",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public partial class Outer<T>
                {
                    public static partial class Inner
                    {
                        [CompressString("hello")]
                        public static partial string Value { get; }
                    }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.GenericTypesUnsupported.Id,
                DiagnosticMessageExpected =
                [
                    Const.CompressStringAttributeShortName,
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports warning for member nested inside a generic outer type",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample<T>
                {
                    [CompressFile("data.txt")]
                    public static partial string Value { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.GenericTypesUnsupported.Id,
                DiagnosticMessageExpected =
                [
                    Const.CompressFileAttributeShortName,
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports warning for CompressFile on a property in a generic class",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample<T>
                {
                    [CompressString("hello")]
                    public static partial string Value();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.GenericTypesUnsupported.Id,
                DiagnosticMessageExpected =
                [
                    Const.CompressStringAttributeShortName,
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports warning for CompressString on a method in a generic class",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample<T>
                {
                    [CompressString("hello")]
                    public static partial string Value { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.GenericTypesUnsupported.Id,
                DiagnosticMessageExpected =
                [
                    Const.CompressStringAttributeShortName,
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports warning for CompressString on a property in a generic class",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                [TarCompress]
                public static partial class Sample<T>
                {
                }
                """,
                DiagnosticId = DiagnosticDescriptors.GenericTypesUnsupported.Id,
                DiagnosticMessageExpected =
                [
                    Const.TarCompressAttributeShortName,
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports warning for TarCompress on a generic class",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
