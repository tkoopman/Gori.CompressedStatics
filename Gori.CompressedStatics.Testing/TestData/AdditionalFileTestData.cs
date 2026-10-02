using System.Collections;

namespace Gori.CompressedStatics.Testing.TestData;

internal class AdditionalFileTestData : IEnumerable<TheoryDataRow<MockSourceData>>
{
    private const string Category = "Analyzer + CodeFix: Additional File Tests";

    public IEnumerator<TheoryDataRow<MockSourceData>> GetEnumerator()
    {
        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile("TestAssets\\AdditionalFiles\\content\\project-root.txt", RelativeToProject = true)]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["TestAssets\\AdditionalFiles\\content\\project-root.txt"],
                DiagnosticId = null,
                DiagnosticIdIsExpected = false,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostic when valid file relative to project",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile("file.txt", RelativeToProject = false)]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["src\\file.txt"],
                DiagnosticId = null,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "No diagnostic when valid file relative to file",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile("content\\file.txt", RelativeToProject = false)]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["content\\file.txt"],
                DiagnosticId = DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
                CodeFixShouldOffer = true,
                CodeFixTitle = "Set RelativeToProject = true",
                CodeFixExpected = [
                    """[CompressFile("content\\file.txt", RelativeToProject = true)]"""
                ],
                CodeFixNotExpected = [
                    """[CompressFile("content\\file.txt")]""",
                    """[CompressFile("content\\file.txt", RelativeToProject = false)]"""
                ],
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise diagnostic when file is not found relative to file, but is if relative to project (RelativeToProject explicitly set to false)",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile("content\\file.txt")]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["content\\file.txt"],
                DiagnosticId = DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
                CodeFixShouldOffer = true,
                CodeFixTitle = "Set RelativeToProject = true",
                CodeFixExpected = [
                    """[CompressFile("content\\file.txt", RelativeToProject = true)]"""
                ],
                CodeFixNotExpected = [
                    """[CompressFile("content\\file.txt")]""",
                    """[CompressFile("content\\file.txt", RelativeToProject = false)]"""
                ],
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise diagnostic when file is not found relative to file, but is if relative to project",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile(file: "content\\file.txt")]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["content\\file.txt"],
                DiagnosticId = DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
                CodeFixShouldOffer = true,
                CodeFixTitle = "Set RelativeToProject = true",
                CodeFixExpected = [
                    """[CompressFile(file: "content\\file.txt", RelativeToProject = true)]"""
                ],
                CodeFixNotExpected = [
                    """[CompressFile("content\\file.txt")]""",
                    """[CompressFile("content\\file.txt", RelativeToProject = false)]""",
                    """[CompressFile(file: "content\\file.txt")]""",
                    """[CompressFile(file: "content\\file.txt", RelativeToProject = false)]""",
                ],
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise diagnostic when file is not found relative to file, but is if relative to project (Named file attribute)",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile(@"content\file.txt", RelativeToProject = false)]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["content\\file.txt"],
                DiagnosticId = DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
                CodeFixShouldOffer = true,
                CodeFixTitle = "Rewrite file path",
                CodeFixExpected = [
                    """[CompressFile("..\\content\\file.txt", RelativeToProject = false)]"""
                ],
                CodeFixNotExpected = [
                    """[CompressFile("content\\file.txt", RelativeToProject = false)]""",
                    """[CompressFile(@"content\file.txt", RelativeToProject = false)]""",
                ],
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise diagnostic when file is not found relative to file, but is if relative to project (Rewrite file path with explicit RelativeToProject)",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile(file: @"content\file.txt")]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["content\\file.txt"],
                DiagnosticId = DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
                CodeFixShouldOffer = true,
                CodeFixTitle = "Rewrite file path",
                CodeFixExpected = [
                    """[CompressFile(file: "..\\content\\file.txt")]"""
                ],
                CodeFixNotExpected = [
                    """[CompressFile(file: @"content\file.txt")]""",
                ],
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise diagnostic when file is not found relative to file, but is if relative to project (Rewrite file path, named file argument)",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile("content\\file.txt")]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["content\\file.txt"],
                DiagnosticId = DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
                CodeFixShouldOffer = true,
                CodeFixTitle = "Rewrite file path",
                CodeFixExpected = [
                    """[CompressFile("..\\content\\file.txt")]"""
                ],
                CodeFixNotExpected = [
                    """[CompressFile("content\\file.txt")]""",
                ],
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise diagnostic when file is not found relative to file, but is if relative to project (Rewrite file path)",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile("..\\content\\file.txt", RelativeToProject = true)]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["content\\file.txt"],
                DiagnosticId = DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
                CodeFixShouldOffer = true,
                CodeFixTitle = "Remove RelativeToProject",
                CodeFixExpected = [
                    """[CompressFile("..\\content\\file.txt")]"""
                ],
                CodeFixNotExpected = [
                    """[CompressFile("..\\content\\file.txt", RelativeToProject = true)]"""
                ],
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise diagnostic when file is not found relative to project, but is if relative to file",
        };

        yield return new TheoryDataRow<MockSourceData>
        (
            new MockSourceData
            {
                SourceCode =
                """
                using Gori.CompressedStatics;
                
                public static partial class Sample
                {
                    [CompressFile("..\\content\\file.txt", RelativeToProject = true)]
                    public static partial string Data { get; }
                }
                """,
                SourcePath = "src\\Sample.cs",
                AdditionalFiles = ["content\\file.txt"],
                DiagnosticId = DiagnosticDescriptors.SourceFileMustBeAdditionalFile.Id,
                DiagnosticMessageExpected = null,
                DiagnosticMessageNotExpected = null,
                CodeFixShouldOffer = true,
                CodeFixTitle = "Rewrite file path",
                CodeFixExpected = [
                    """[CompressFile("content\\file.txt", RelativeToProject = true)]"""
                ],
                CodeFixNotExpected = [
                    """[CompressFile("..\\content\\file.txt", RelativeToProject = true)]"""
                ],
            }
        )
        {
            Traits = { { "Category", [Category] } },
            Label = "Raise diagnostic when file is not found relative to project, but is if relative to file (Rewrite file path)",
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
