using System.Collections;

using Gori.Common.Roslyn.Test;

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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBeStatic)
                    {
                        IsExpected = true,
                        Message = "'GetText'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public static partial string GetText" },
                            new CodeFixExpectation { IsExpected = false, Code = "public partial string GetText" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBeStatic) { IsExpected = true, Message = $"[{Const.CompressStringAttributeShortName}]" },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBeStatic)
                    {
                        IsExpected = true,
                        Message = "'Value'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public static partial string Value" },
                            new CodeFixExpectation { IsExpected = false, Code = "public partial string Value" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBeStatic) { IsExpected = true, Message = $"[{Const.CompressStringAttributeShortName}]" },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBeStatic)
                    {
                        IsExpected = true,
                        Message = "'Data'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public static partial string Data" },
                            new CodeFixExpectation { IsExpected = false, Code = "public partial string Data" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBeStatic) { IsExpected = true, Message = $"[{Const.CompressFileAttributeShortName}]" },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBeStatic)
                    {
                        IsExpected = true,
                        Message = "'Value'",
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Code = "public static partial string Value" },
                            new CodeFixExpectation { IsExpected = false, Code = "public partial string Value" },
                        ],
                    },
                    new DiagnosticExpectation(DiagnosticDescriptors.MemberMustBeStatic) { IsExpected = true, Message = $"[{Const.CompressStringAttributeShortName}]" },
                ],
            })
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise error for TarCompress + CompressString on non-static property (with static constructor)",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
