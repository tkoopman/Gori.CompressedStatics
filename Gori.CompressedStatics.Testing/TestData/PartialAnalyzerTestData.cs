using System.Collections;

using Gori.Common.Roslyn.Test;

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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartial)
                    {
                        IsExpected = true,
                        Message = "'GetText'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public static partial string GetText();" },
                            new CodeFixExpectation { IsExpected = false, Code = "public static string GetText();" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartial) { IsExpected = true, Message = $"[{Const.CompressStringAttributeShortName}]" },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartial)
                    {
                        IsExpected = true,
                        Message = "'Value'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public static partial string Value" },
                            new CodeFixExpectation { IsExpected = false, Code = "public static string Value" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartial) { IsExpected = true, Message = $"[{Const.CompressStringAttributeShortName}]" },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartial)
                    {
                        IsExpected = true,
                        Message = "'Data'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public static partial string Data" },
                            new CodeFixExpectation { IsExpected = false, Code = "public static string Data" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartial) { IsExpected = true, Message = $"[{Const.CompressFileAttributeShortName}]" },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.TypeMustBePartial)
                    {
                        IsExpected = true,
                        Message = "'Sample'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public partial class Sample" },
                            new CodeFixExpectation { IsExpected = false, Code = "public class Sample" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.TypeMustBePartial) { IsExpected = true, Message = $"[{Const.TarCompressAttributeShortName}]" },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartialWhenTarHasStaticConstructor)
                    {
                        IsExpected = true,
                        Message = "Member 'Value'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public static partial string Value" },
                            new CodeFixExpectation { IsExpected = false, Code = "public static string Value" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBePartialWhenTarHasStaticConstructor) { IsExpected = true, Message = "'Sample' declares" },
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
                Expectations = [DiagnosticExpectation.NoOthers],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostic for TarCompress + CompressString on non-partial property (with auto-generated static constructor)",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
