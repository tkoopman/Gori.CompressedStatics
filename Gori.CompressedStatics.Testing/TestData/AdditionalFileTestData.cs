using System.Collections;

using Gori.Common.Roslyn.Test;

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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations = [DiagnosticExpectation.NoOthers],
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.SourceFileMustBeAdditionalFile)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Title = "Set RelativeToProject = true", EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("content\\file.txt", RelativeToProject = true)]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("content\\file.txt")]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("content\\file.txt", RelativeToProject = false)]""" },
                        ],
                    },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.SourceFileMustBeAdditionalFile)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Title = "Set RelativeToProject = true", EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("content\\file.txt", RelativeToProject = true)]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("content\\file.txt")]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("content\\file.txt", RelativeToProject = false)]""" },
                        ],
                    },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.SourceFileMustBeAdditionalFile)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Title = "Set RelativeToProject = true", EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile(file: "content\\file.txt", RelativeToProject = true)]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("content\\file.txt")]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("content\\file.txt", RelativeToProject = false)]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile(file: "content\\file.txt")]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile(file: "content\\file.txt", RelativeToProject = false)]""" },
                        ],
                    },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.SourceFileMustBeAdditionalFile)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Title = "Rewrite file path", EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile("..\\content\\file.txt", RelativeToProject = false)]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile("content\\file.txt", RelativeToProject = false)]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile(@"content\file.txt", RelativeToProject = false)]""" },
                        ],
                    },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.SourceFileMustBeAdditionalFile)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Title = "Rewrite file path", EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile(file: "..\\content\\file.txt")]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile(file: @"content\file.txt")]""" },
                        ],
                    },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.SourceFileMustBeAdditionalFile)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Title = "Rewrite file path", EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile("..\\content\\file.txt")]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile("content\\file.txt")]""" },
                        ],
                    },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.SourceFileMustBeAdditionalFile)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Title = "Remove RelativeToProject", EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("..\\content\\file.txt")]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.ToggleRelativeToProject", Code = """[CompressFile("..\\content\\file.txt", RelativeToProject = true)]""" },
                        ],
                    },
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
                Expectations =
                [
                    new DiagnosticExpectation(DiagnosticDescriptors.SourceFileMustBeAdditionalFile)
                    {
                        IsExpected = true,
                        CodeFixExpectations =
                        [
                            CodeFixExpectation.NotEmpty,
                            new CodeFixExpectation { IsExpected = true, Title = "Rewrite file path", EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile("content\\file.txt", RelativeToProject = true)]""" },
                            new CodeFixExpectation { IsExpected = false, EquivalenceKey = "CompressFileAttribute.RewriteFilePath", Code = """[CompressFile("..\\content\\file.txt", RelativeToProject = true)]""" },
                        ],
                    },
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
