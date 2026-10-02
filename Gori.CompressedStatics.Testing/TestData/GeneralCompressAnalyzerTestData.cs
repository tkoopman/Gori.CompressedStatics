using System.Collections;

namespace Gori.CompressedStatics.Testing.TestData;

/// <summary>
/// Provides test data for the <see cref="AnalyzerTests.CompressAttributeAnalyzerTest"/> tests.
/// </summary>
internal sealed class GeneralCompressAnalyzerTestData : IEnumerable<TheoryDataRow<MockSourceData>>
{
    private const string Category = "Analyzer: General Compress Attribute Tests";

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
                    [CompressFile("data.txt")]
                    [CompressString("hello")]
                    public static partial string GetText();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.OneAttributePerMember.Id,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raises error for multiple compressed attributes on a single member",
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
                    public static partial int GetValue();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustReturnStringOrByteArray.Id,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raises error for CompressString on int return type",
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
                    public static partial object GetValue();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustReturnStringOrByteArray.Id,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raises error for CompressString on object return type",
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
                    public static partial string? GetValue();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustReturnStringOrByteArray.Id,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raises error for CompressFile on nullable string return type",
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
                    public static partial string? GetValue();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustReturnStringOrByteArray.Id,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raises error for CompressString on nullable string return type",
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
                    public static partial string GetData(int value);
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MethodMustHaveNoParameters.Id,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raises error for CompressFile on method with parameters",
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
                    public static partial string GetText(int value);
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MethodMustHaveNoParameters.Id,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raises error for CompressString on method with parameters",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
