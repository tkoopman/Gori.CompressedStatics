using System.Collections;

namespace Gori.CompressedStatics.Testing.TestData;

/// <summary>
/// Provides test data for the <see cref="AnalyzerTests.AddPartialModifierCodeFixProviderTest"/> tests.
/// </summary>
internal sealed class PartialAnalyzerTestData : IEnumerable<TheoryDataRow<MockSourceData>>
{
    private const string Category = "Analyzer + CodeFix: Partial Tests";
    public IEnumerator<TheoryDataRow<MockSourceData>> GetEnumerator()
    {
        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
            
                [TarCompress]
                public partial class Sample
                {
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for TarCompress on partial class",
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
                    [CompressString("hello")]
                    public static string Value { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostics for TarCompress + CompressString on non-partial property (No static constructor)",
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
                    public static string GetText();
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustBePartial.Id,
                DiagnosticMessageExpected =
                [
                    "'GetText'",
                    $"[{Const.CompressStringAttributeShortName}]",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "public static partial string GetText();",
                ],
                CodeFixNotExpected =
                [
                    "public static string GetText();",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports error for CompressString on non-partial method",
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
                    public static string Value { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustBePartial.Id,
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
                    "public static string Value",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports error for CompressString on non-partial property",
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
                    public static string Data { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustBePartial.Id,
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
                    "public static string Data",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports error for CompressFile on non-partial property",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
            
                [TarCompress]
                public class Sample
                {
                }
                """,
                DiagnosticId = DiagnosticDescriptors.TypeMustBePartial.Id,
                DiagnosticMessageExpected =
                [
                    "'Sample'",
                    $"[{Const.TarCompressAttributeShortName}]",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "public partial class Sample",
                ],
                CodeFixNotExpected =
                [
                    "public class Sample",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Reports error for TarCompress on non-partial class",
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
                    public static string Value { get; }
                }
                """,
                DiagnosticId = DiagnosticDescriptors.MemberMustBePartialWhenTarHasStaticConstructor.Id,
                DiagnosticMessageExpected =
                [
                    "Member 'Value'",
                    "'Sample' declares",
                ],
                CodeFixShouldOffer = true,
                CodeFixExpected =
                [
                    "public static partial string Value",
                ],
                CodeFixNotExpected =
                [
                    "public static string Value",
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise error for TarCompress + CompressString on non-partial property (with static constructor)",
        };

        yield return new TheoryDataRow<MockSourceData>(
            new MockSourceData()
            {
                SourceCode =
                """
                // <auto-generated />
                using Gori.CompressedStatics;
                
                [TarCompress]
                public partial class Sample
                {
                    static Sample()
                    {
                    }

                    [CompressString("hello")]
                    public static string Value { get; }
                }
                """,
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostic for TarCompress + CompressString on non-partial property (with auto-generated static constructor)",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
