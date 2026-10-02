using System.Collections;

namespace Gori.CompressedStatics.Testing.TestData;

/// <summary>
/// Provides test data for the <see cref="AnalyzerTests.CompressAttributeAnalyzerTest"/> tests.
/// </summary>
internal sealed class StaticPartialAnalyzerTestData : IEnumerable<TheoryDataRow<MockSourceData>>
{
    private const string Category = "Analyzer: Static Partial Tests";
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
                    [CompressString("hello")]
                    public static partial string GetText();
                }
                """,
                DiagnosticId = null,
                DiagnosticMessageExpected =
                [
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for CompressString on static partial method",
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
                DiagnosticId = null,
                DiagnosticMessageExpected =
                [
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for CompressString on static partial property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
            
                public static partial class Sample
                {
                    [CompressFile("data.txt")]
                    public static partial string Data { get; }
                }
                """,
                DiagnosticId = null,
                DiagnosticMessageExpected =
                [
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for CompressFile on static partial property",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
