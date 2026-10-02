using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using Gori.CompressedStatics.Testing.TestData;

using Gori.Roslyn.Testing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Gori.CompressedStatics.Testing;

internal class MockProject(
    LanguageVersion languageVersion = LanguageVersion.Preview,
    CancellationToken cancellationToken = default)
{
    public const string ProjectDirectory = @"c:\CompressedStaticsTests\ProjectRoot";
    public const string ProjectDirectoryWithSeparator = @"c:\CompressedStaticsTests\ProjectRoot\";
    public const string EscapedProjectDirectory = @"c:\\CompressedStaticsTests\\ProjectRoot";
    public const string DefaultSourcePath = "source.cs";

    public static IEnumerable<PortableExecutableReference> References { get; }
        = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
                              .Split(Path.PathSeparator)
                              .Select(path => MetadataReference.CreateFromFile(path))
                              .Concat([MetadataReference.CreateFromFile(typeof(CompressFileAttribute).Assembly.Location)]);

    /// <summary>
    /// Initializes a new instance of the <see cref="MockProject"/> class with the specified source code.
    /// </summary>
    /// <param name="source">The source code to add to the project.</param>
    /// <param name="languageVersion">The C# language version to use for the project.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public MockProject(
        string source,
        LanguageVersion languageVersion = LanguageVersion.Preview,
        CancellationToken cancellationToken = default)
        : this(languageVersion, cancellationToken) => _ = AddDocument(source);

    /// <summary>
    /// Initializes a new instance of the <see cref="MockProject"/> class with the specified source code and additional files.
    /// </summary>
    /// <param name="source">The source code to add to the project.</param>
    /// <param name="additionalFiles">A collection of additional file paths to add to the project.</param>
    /// <param name="languageVersion">The C# language version to use for the project.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public MockProject(
        string source,
        IEnumerable<string> additionalFiles,
        LanguageVersion languageVersion = LanguageVersion.Preview,
        CancellationToken cancellationToken = default)
        : this(languageVersion, cancellationToken)
    {
        _ = AddDocument(source);
        _ = AddAdditionalFiles(additionalFiles);
    }

    /// <summary>
    /// Cancellation token used for asynchronous operations within the <see cref="MockProject"/> class.
    /// Should be set to current test's cancellation token to ensure proper test cancellation behavior.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = cancellationToken;

    private Project Project { get; set; } = CreateProject(languageVersion);

    /// <summary>
    /// Adds an additional file to the project with the specified path.
    /// The content of the file will be set to the string representation of the current number of additional files in the project.
    /// So assuming all additional files added by this, then all will have unique contents starting at "0".
    /// </summary>
    /// <param name="path">The path of the additional file to add, relative to the project root.</param>
    /// <returns>The current <see cref="MockProject"/> instance.</returns>
    public MockProject AddAdditionalFile(string path)
        => AddAdditionalFile(path, Project.AnalyzerOptions.AdditionalFiles.Length.ToString());

    /// <summary>
    /// Adds an additional file to the project with the specified path and content.
    /// </summary>
    /// <param name="path">The path of the additional file to add, relative to the project root.</param>
    /// <param name="content">The content of the additional file.</param>
    /// <returns>The current <see cref="MockProject"/> instance.</returns>
    public MockProject AddAdditionalFile(string path, string content)
    {
        (string name, IEnumerable<string> folders, string absolutePath) = GetFileInfo(path);
        Project = Project.AddAdditionalDocument(name, text: content, folders: folders, filePath: absolutePath).Project;
        return this;
    }

    /// <summary>
    /// Adds multiple additional files to the project with the specified paths.
    /// Contents of each file added follows <see cref="AddAdditionalFile(string)"/>
    /// </summary>
    /// <param name="additionalFiles">The paths of the additional files to add, relative to the project root.</param>
    /// <returns>The current <see cref="MockProject"/> instance.</returns>
    public MockProject AddAdditionalFiles(IEnumerable<string>? additionalFiles)
    {
        if (additionalFiles is not null)
        {
            foreach (string file in additionalFiles)
            {
                _ = AddAdditionalFile(file);
            }
        }

        return this;
    }

    /// <summary>
    /// Adds multiple additional files to the project with the specified paths and contents.
    /// </summary>
    /// <param name="additionalFiles">The paths and contents of the additional files to add, relative to the project root.</param>
    /// <returns>The current <see cref="MockProject"/> instance.</returns>
    public MockProject AddAdditionalFiles(IEnumerable<KeyValuePair<string, string>>? additionalFiles)
    {
        if (additionalFiles is not null)
        {
            foreach (KeyValuePair<string, string> file in additionalFiles)
            {
                _ = AddAdditionalFile(file.Key, file.Value);
            }
        }

        return this;
    }

    /// <summary>
    /// Adds a document to the project with the specified source code and optional source path.
    /// </summary>
    /// <param name="source">The source code of the document.</param>
    /// <param name="sourcePath">The optional path of the document, relative to the project root.
    /// If not specified, a default path will be used (<see cref="DefaultSourcePath"/>).</param>
    /// <returns>The added <see cref="Document"/> instance.</returns>
    public Document AddDocument(string source, string? sourcePath = null)
    {
        (string name, IEnumerable<string> folders, string absolutePath) = GetFileInfo(sourcePath);
        var sourceText = SourceText.From(source);

        Document document = Project.AddDocument(name, sourceText, folders: folders, filePath: absolutePath);
        Project = document.Project;

        return document;
    }

    /// <summary>
    /// Asserts the diagnostics produced by the specified <see cref="DiagnosticAnalyzer"/> for the given mock source data.
    /// </summary>
    /// <typeparam name="TDiag">The type of the <see cref="DiagnosticAnalyzer"/> to test.</typeparam>
    /// <param name="data">The mock source data to test.</param>
    /// <param name="output">The test output helper.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task AssertAnalyserTests<TDiag>(MockSourceData data, ITestOutputHelper output)
        where TDiag : DiagnosticAnalyzer, new()
        => await AssertAnalyserTests<TDiag, NoCodeFixProvider>(data, output);

    /// <summary>
    /// Asserts the diagnostics and code fixes produced by the specified <see cref="DiagnosticAnalyzer"/>
    /// and <see cref="CodeFixProvider"/> for the given mock source data.
    /// </summary>
    /// <typeparam name="TDiag">The type of the <see cref="DiagnosticAnalyzer"/> to test.</typeparam>
    /// <typeparam name="TCodeFix">The type of the <see cref="CodeFixProvider"/> to test.</typeparam>
    /// <param name="data">The mock source data to test.</param>
    /// <param name="output">The test output helper.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task AssertAnalyserTests<TDiag, TCodeFix>(MockSourceData data, ITestOutputHelper output)
        where TDiag : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var project = new MockProject(cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddAdditionalFiles(data.AdditionalFiles)
                   .AddAdditionalFiles(data.AdditionalFilesWithContent);

        Document document = project.AddDocument(data.SourceCode, data.SourcePath);

        ImmutableArray<Diagnostic> diagnostics = await project.GetDiagnosticsAsync<TDiag>();
        output.Write(diagnostics);

        // Explicit check for a single ID not being raised
        if (data.DiagnosticId is not null && data.DiagnosticIdIsExpected is false)
        {
            Assert.DoesNotContain(diagnostics, d => d.Id == data.DiagnosticId);
            return;
        }

        // General check for no diagnostics being raised
        if (data.DiagnosticId is null && data.DiagnosticMessageExpected is null && data.DiagnosticMessageNotExpected is null)
        {
            Assert.Empty(diagnostics);
            return;
        }

        string[] messages = [];

        Diagnostic? diagnostic = null;

        if (data.DiagnosticId is not null)
        {  // Check for a single diagnostic with the expected ID
            diagnostic = Assert.Single(diagnostics, d => d.Id == data.DiagnosticId);
            messages = [diagnostic.GetMessage(CultureInfo.InvariantCulture)];
        }
        else
        {  // Check for any diagnostics and get their messages for message checks only
            messages = [.. diagnostics.Select(d => d.GetMessage(CultureInfo.InvariantCulture))];
        }

        if (data.DiagnosticMessageExpected is not null)
        {
            // Must have some messages if we are checking for expected messages
            Assert.NotEmpty(messages);

            foreach (string expectedText in data.DiagnosticMessageExpected)
            {
                Assert.Contains(messages, s => s.Contains(expectedText, StringComparison.Ordinal));
            }
        }

        if (data.DiagnosticMessageNotExpected is not null)
        {
            foreach (string notExpectedText in data.DiagnosticMessageNotExpected)
            {
                Assert.DoesNotContain(messages, s => s.Contains(notExpectedText, StringComparison.Ordinal));
            }
        }

        if (typeof(TCodeFix) == typeof(NoCodeFixProvider))
        {
            return;
        }

        if (diagnostic is null)
        {   // Do not check code fixes if no diagnostic was found
            output.WriteLine("No diagnostic found, skipping code fix checks.");
            Assert.Null(data.CodeFixExpected);
            return;
        }

        ImmutableArray<CodeAction> actions = await project.GetCodeFixActionsAsync<TCodeFix>(document, diagnostic);
        output.Write(actions);

        if (data.CodeFixShouldOffer is false)
        {
            Assert.Empty(actions);
            return;
        }

        if (data.CodeFixShouldOffer is true)
        {
            Assert.NotEmpty(actions);
        }

        List<string> updatedCode = [];
        if (data.CodeFixTitle is not null)
        {
            CodeAction action = Assert.Single(actions, a => a.Title.Contains(data.CodeFixTitle, StringComparison.Ordinal));
            ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
            ApplyChangesOperation applyChanges = Assert.IsType<ApplyChangesOperation>(Assert.Single(operations));
            Document? updatedDocument = applyChanges.ChangedSolution.GetDocument(document.Id);
            Assert.NotNull(updatedDocument);
            updatedCode = [(await updatedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString()];
            output.WriteLine($"\nUpdated source ({data.CodeFixTitle}):\n" + updatedCode[0]);
        }
        else
        {
            foreach (CodeAction action in actions)
            {
                ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
                ApplyChangesOperation applyChanges = Assert.IsType<ApplyChangesOperation>(Assert.Single(operations));
                Document? updatedDocument = applyChanges.ChangedSolution.GetDocument(document.Id);
                Assert.NotNull(updatedDocument);
                string updatedSource = (await updatedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
                output.WriteLine($"\nUpdated source ({action.Title}):\n" + updatedSource);
                updatedCode.Add(updatedSource);
            }
        }

        if (data.CodeFixExpected is not null)
        {
            foreach (string expectedText in data.CodeFixExpected)
            {
                Assert.Contains(updatedCode, s => s.Contains(expectedText, StringComparison.Ordinal));
            }
        }

        if (data.CodeFixNotExpected is not null)
        {
            foreach (string notExpectedText in data.CodeFixNotExpected)
            {
                Assert.DoesNotContain(updatedCode, s => s.Contains(notExpectedText, StringComparison.Ordinal));
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="GeneratorDriver"/> for the specified incremental generator type.
    /// </summary>
    /// <typeparam name="T">The type of the incremental generator.</typeparam>
    /// <param name="excludeProjectDirectoryProperty">Whether to exclude the project directory property.</param>
    /// <returns>The created <see cref="GeneratorDriver"/>.</returns>
    public GeneratorDriver CreateGeneratorDriver<T>(bool excludeProjectDirectoryProperty = false)
        where T : IIncrementalGenerator, new()
        => CSharpGeneratorDriver.Create(
            generators: [new T().AsSourceGenerator()],
            additionalTexts: Project.AnalyzerOptions.AdditionalFiles,
            optionsProvider: CreateOptionsProvider(excludeProjectDirectoryProperty),
            parseOptions: (CSharpParseOptions?)Project.ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    /// <summary>
    /// Creates a new <see cref="Project"/> instance with the specified C# language version.
    /// </summary>
    /// <param name="languageVersion">The C# language version to use for the project.</param>
    /// <returns>The created <see cref="Project"/> instance.</returns>
    public static Project CreateProject(LanguageVersion languageVersion = LanguageVersion.Preview)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name: nameof(MockProject),
            assemblyName: nameof(MockProject),
            language: LanguageNames.CSharp,
            parseOptions: new CSharpParseOptions(languageVersion),
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: References);

        Project project = workspace.AddProject(projectInfo);

        return project;
    }

    /// <summary>
    /// Gets the compilation for the current fake project.
    /// </summary>
    /// <returns>The compilation for the current fake project.</returns>
    public async Task<Compilation> GetCompilationAsync()
        => (await Project.GetCompilationAsync(CancellationToken))!;

    /// <summary>
    /// Gets the diagnostics produced by the given <see cref="DiagnosticAnalyzer"/> for the current fake project.
    /// </summary>
    /// <typeparam name="T">The type of the diagnostic analyzer.</typeparam>
    /// <returns>The diagnostics produced by the analyzer.</returns>
    public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync<T>(bool excludeProjectDirectoryProperty = false)
        where T : DiagnosticAnalyzer, new()
    {
        Compilation compilation = (await Project.GetCompilationAsync(CancellationToken))!;
        DiagnosticAnalyzer analyzer = new T();
        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            options: new AnalyzerOptions(Project.AnalyzerOptions.AdditionalFiles, CreateOptionsProvider(excludeProjectDirectoryProperty)));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken);
    }

    /// <summary>
    /// Gets the code fix actions provided by the given <see cref="CodeFixProvider"/> for the specified document and diagnostic.
    /// </summary>
    /// <typeparam name="T">The type of the code fix provider.</typeparam>
    /// <param name="document">The document for which to get code fix actions.</param>
    /// <param name="diagnostic">The diagnostic for which to get code fix actions.</param>
    /// <returns>The code fix actions provided by the code fix provider.</returns>
    public async Task<ImmutableArray<CodeAction>> GetCodeFixActionsAsync<T>(Document document, Diagnostic diagnostic)
        where T : CodeFixProvider, new()
    {
        CodeFixProvider codeFixProvider = new T();
        List<CodeAction> actions = [];

        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken);

        await codeFixProvider.RegisterCodeFixesAsync(context);
        return [.. actions];
    }

    /// <summary>
    /// Runs a single code fix action provided by the given <see cref="CodeFixProvider"/> for the specified document and diagnostic.
    /// </summary>
    /// <typeparam name="T">The type of the code fix provider.</typeparam>
    /// <param name="document">The document for which to run the code fix.</param>
    /// <param name="diagnostic">The diagnostic for which to run the code fix.</param>
    /// <param name="actionTitle">The title of the code fix action to run. If null, expects a single code fix action.</param>
    /// <returns>The updated document after applying the code fix, or null if the code fix could not be applied.</returns>
    public async Task<Document?> RunSingleCodeFix<T>(Document document, Diagnostic diagnostic, string? actionTitle = null)
        where T : CodeFixProvider, new()
    {
        ImmutableArray<CodeAction> actions = await GetCodeFixActionsAsync<T>(document, diagnostic);
        CodeAction? actionToApply;

        if (actionTitle is null)
        {
            if (actions.Length != 1)
            {
                TestContext.Current.AddWarning($"Expected a single code fix action, but found {actions.Length}. Diagnostic: {diagnostic.Id} at {diagnostic.Location.GetLineSpan()}");
                return null;
            }

            actionToApply = actions[0];
        }
        else
        {
            actionToApply = actions.FirstOrDefault(action => string.Equals(action.Title, actionTitle, StringComparison.Ordinal));
            if (actionToApply is null)
            {
                TestContext.Current.AddWarning($"Expected a code fix action titled '{actionTitle}', but found {actions.Length} actions. Diagnostic: {diagnostic.Id} at {diagnostic.Location.GetLineSpan()}");
                return null;
            }
        }

        ImmutableArray<CodeActionOperation> operations = await actionToApply.GetOperationsAsync(CancellationToken);
        if (operations.Length != 1 || operations[0] is not ApplyChangesOperation applyChanges)
        {
            TestContext.Current.AddWarning($"Expected a single ApplyChangesOperation, but found {operations.Length} operations. Diagnostic: {diagnostic.Id} at {diagnostic.Location.GetLineSpan()}");
            return null;
        }

        Document updatedDocument = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        return updatedDocument;
    }

    /// <summary>
    /// Runs the specified incremental generator and returns a string containing all generated sources.
    /// </summary>
    /// <typeparam name="T">The type of the incremental generator to run.</typeparam>
    /// <param name="excludeProjectDirectoryProperty">Whether to exclude the project directory property from the analyzer options.</param>
    /// <returns>A string containing all generated sources.</returns>
    public async Task<string> RunGeneratorDumpAll<T>(bool excludeProjectDirectoryProperty = false)
        where T : IIncrementalGenerator, new()
    {
        GeneratorDriverRunResult runResult = await RunGenerator<T>(excludeProjectDirectoryProperty);
        if (runResult.Results.Length != 1)
        {
            TestContext.Current.AddWarning($"Expected a single generator result, but found {runResult.Results.Length}.");
        }

        var buffer = new StringBuilder();
        foreach (GeneratorRunResult generatorRunResult in runResult.Results)
        {
            foreach (GeneratedSourceResult generatedSource in generatorRunResult.GeneratedSources)
            {
                _ = buffer.AppendLine($"// Generated source: {generatedSource.HintName}")
                          .AppendLine(generatedSource.SourceText.ToString())
                          .AppendLine();
            }
        }

        return buffer.ToString();
    }

    /// <summary>
    /// Runs the specified incremental generator and returns the run result.
    /// </summary>
    /// <typeparam name="T">The type of the incremental generator to run.</typeparam>
    /// <param name="excludeProjectDirectoryProperty">Whether to exclude the project directory property from the analyzer options.</param>
    /// <returns>The run result of the generator.</returns>
    public async Task<GeneratorDriverRunResult> RunGenerator<T>(bool excludeProjectDirectoryProperty = false)
        where T : IIncrementalGenerator, new()
    {
        Compilation compilation = await GetCompilationAsync();
        GeneratorDriver driver = CreateGeneratorDriver<T>(excludeProjectDirectoryProperty);

        driver = driver.RunGenerators(compilation, cancellationToken: CancellationToken);
        return driver.GetRunResult();
    }

    /// <summary>
    /// Creates an <see cref="AnalyzerConfigOptionsProvider"/> with optional exclusion of the project directory property.
    /// </summary>
    /// <param name="excludeProjectDirectoryProperty">Whether to exclude the project directory property from the analyzer options.</param>
    /// <returns>The created <see cref="AnalyzerConfigOptionsProvider"/>.</returns>
    public static AnalyzerConfigOptionsProvider CreateOptionsProvider(bool excludeProjectDirectoryProperty = false)
        => new InMemoryAnalyzerConfigOptionsProvider(
            excludeProjectDirectoryProperty
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["build_property.MSBuildProjectDirectory"] = excludeProjectDirectoryProperty ? string.Empty : ProjectDirectory
                });

    /// <summary>
    /// Gets the file information (name, folders, absolute path) for the specified file path.
    /// </summary>
    /// <param name="filePath">The file path to get information for.</param>
    /// <returns>A tuple containing the file name, folders, and absolute path.</returns>
    private static (string FileName, IEnumerable<string> Folders, string AbsolutePath) GetFileInfo(string? filePath)
    {
        string absolutePath = Path.GetFullPath(Path.Combine(ProjectDirectory, string.IsNullOrWhiteSpace(filePath) ? DefaultSourcePath : filePath));
        string name = Path.GetFileName(absolutePath);
        bool isInProjectDirectory = absolutePath.StartsWith(ProjectDirectoryWithSeparator, StringComparison.OrdinalIgnoreCase);
        List<string> folders = [];

        if (isInProjectDirectory)
        {
            string relativePath = absolutePath[ProjectDirectoryWithSeparator.Length..];
            string? directory = Path.GetDirectoryName(relativePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                folders.AddRange(directory.Split(Path.DirectorySeparatorChar));
            }
        }

        return (name, folders, absolutePath);
    }

    private sealed class InMemoryAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
    {
        private static readonly AnalyzerConfigOptions Empty = new InMemoryAnalyzerConfigOptions(new Dictionary<string, string>(StringComparer.Ordinal));
        private readonly AnalyzerConfigOptions globalOptions = new InMemoryAnalyzerConfigOptions(globalOptions);

        public override AnalyzerConfigOptions GlobalOptions => globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Empty;
    }

    private sealed class InMemoryAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> values = values;

        public override bool TryGetValue(string key, out string value)
            => values.TryGetValue(key, out value!);
    }

    private sealed class NoCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [];
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
            => Task.CompletedTask;
    }
}
