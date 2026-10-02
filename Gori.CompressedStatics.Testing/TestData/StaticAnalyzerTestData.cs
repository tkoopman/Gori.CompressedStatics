using System.Collections;

namespace Gori.CompressedStatics.Testing.TestData;

/// <summary>
/// Provides test data for the <see cref="AnalyzerTests.AddStaticModifierCodeFixProviderTest"/> tests.
/// </summary>
internal sealed class StaticAnalyzerTestData : IEnumerable<TheoryDataRow<MockSourceData>>
{
    private const string Category = "Analyzer + CodeFix: Static Tests";
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
                    public partial string GetText();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustBeStatic.Id,
                DiagnosticMessageExpected =
                [
                    "'GetText'",
                    $"[{Const.CompressStringAttributeShortName}]",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "public static partial string GetText",
                ],
                CodeFixNotExpected =
                [
                    "public partial string GetText",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports error for CompressString on non-static method",
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
                    public partial string Value { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustBeStatic.Id,
                DiagnosticMessageExpected =
                [
                    "'Value'",
                    $"[{Const.CompressStringAttributeShortName}]",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "public static partial string Value",
                ],
                CodeFixNotExpected =
                [
                    "public partial string Value",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports error for CompressString on non-static property",
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
                    public partial string Data { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustBeStatic.Id,
                DiagnosticMessageExpected =
                [
                    "'Data'",
                    $"[{Const.CompressFileAttributeShortName}]",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "public static partial string Data",
                ],
                CodeFixNotExpected =
                [
                    "public partial string Data",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports error for CompressFile on non-static property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
            
                [TarCompress]
                public partial class Sample
                {
                    static Sample()
                    {
                    }

                    [CompressString("hello")]
                    public partial string Value { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustBeStatic.Id,
                DiagnosticMessageExpected =
                [
                    "'Value'",
                    $"[{Const.CompressStringAttributeShortName}]",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "public static partial string Value",
                ],
                CodeFixNotExpected =
                [
                    "public partial string Value",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise error for TarCompress + CompressString on non-static property (with static constructor)",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
