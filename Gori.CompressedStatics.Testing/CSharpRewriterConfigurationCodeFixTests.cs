using System.Collections.Immutable;

using Gori.Common.Roslyn.Test;
using Gori.CompressedStatics.CodeFix;
using Gori.CompressedStatics.CSharp;
using Gori.CompressedStatics.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.Testing;

public sealed class CSharpRewriterConfigurationCodeFixTests(ITestOutputHelper output)
{
    [Fact]
    public async Task BindingIssueAddsAllMissingPolyfillIgnores()
    {
        const string source = "using Gori.CompressedStatics; [assembly: CSharpRewriterIgnore(\"CS0656\", \"System.Runtime.CompilerServices.RequiredMemberAttribute\")] public class Host { }";
        MockProject project = CreateProject(source, out Document document);
        Diagnostic diagnostic = await CreateBindingDiagnostic(
            document,
            "CS0656",
            "Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'.");

        ImmutableArray<CodeAction> actions = await project.GetCodeFixActionsAsync<CSharpRewriterConfigurationCodeFixProvider>(document, diagnostic);
        output.Write(actions);
        CodeAction action = Assert.Single(actions);
        Assert.Equal("Add all required polyfill ignores", action.Title);

        Solution updatedSolution = await ApplyAction(action);
        Document configuration = Assert.Single(updatedSolution.GetProject(document.Project.Id)!.Documents,
            item => item.Name == "CSharpRewriter.Configuration.cs");
        string updatedText = (await configuration.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        output.Write(action, new ApplyChangesOperation(updatedSolution), updatedText);

        Assert.Equal(7, CountOccurrences(updatedText, "[assembly: CSharpRewriterIgnore("));
        Project? updatedProject = updatedSolution.GetProject(document.Project.Id);
        Assert.NotNull(updatedProject);
        Compilation? updatedCompilation = await updatedProject.GetCompilationAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(updatedCompilation);

        foreach (Ignore ignore in Ignore.PolyfillIgnores)
        {
            Assert.Contains(updatedCompilation.Assembly.GetAttributes(), attribute =>
                attribute.AttributeClass?.ToDisplayString() == "Gori.CompressedStatics.CSharpRewriterIgnoreAttribute"
                && attribute.ConstructorArguments.Length > 1
                && StringComparer.Ordinal.Equals(attribute.ConstructorArguments[0].Value as string, ignore.DiagnosticID)
                && StringComparer.Ordinal.Equals(attribute.ConstructorArguments[1].Value as string, ignore.MessageContains));
        }
    }

    [Fact]
    public async Task BindingIssueAddsSpecificIgnoreAndPreservesExistingConfiguration()
    {
        const string host = "public class Host { }";
        const string existingConfiguration = "using Gori.CompressedStatics; [assembly: CSharpRewriterIgnore(\"CS0246\", \"KeepThis\")]";
        var project = new MockProject(typeof(CSharpRewriterIgnoreAttribute).Assembly,
            cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddDocument(existingConfiguration, "CSharpRewriter.Configuration.cs");
        Document document = project.AddDocument(host, "src\\Host.cs");
        Diagnostic diagnostic = await CreateBindingDiagnostic(document, "CS0029", "Cannot implicitly convert type 'string' to 'int'");

        ImmutableArray<CodeAction> actions = await project.GetCodeFixActionsAsync<CSharpRewriterConfigurationCodeFixProvider>(document, diagnostic);
        output.Write(actions);
        CodeAction action = Assert.Single(actions);
        Assert.Equal("Ignore compiler diagnostic CS0029 for cleanup", action.Title);

        Solution updatedSolution = await ApplyAction(action);
        Document configuration = Assert.Single(updatedSolution.GetProject(document.Project.Id)!.Documents,
            item => item.Name == "CSharpRewriter.Configuration.cs");
        string updatedText = (await configuration.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        output.Write(action, new ApplyChangesOperation(updatedSolution), updatedText);

        Assert.Contains("CSharpRewriterIgnore(\"CS0246\", \"KeepThis\")", updatedText, StringComparison.Ordinal);
        Assert.Contains("CSharpRewriterIgnore(\"CS0029\", \"Cannot implicitly convert type 'string' to 'int'\")", updatedText, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(updatedText, "[assembly: CSharpRewriterIgnore("));
    }

    [Fact]
    public async Task UnresolvableTypeAddsPlaceholderMapping()
    {
        MockProject project = CreateProject("public class Host { }", out Document document);
        Diagnostic diagnostic = await CreateUnresolvableDiagnostic(document, "UnresolvableType", "Missing.Context");

        ImmutableArray<CodeAction> actions = await project.GetCodeFixActionsAsync<CSharpRewriterConfigurationCodeFixProvider>(document, diagnostic);
        output.Write(actions);
        CodeAction action = Assert.Single(actions);
        Assert.Equal("Add placeholder type mapping", action.Title);

        Solution updatedSolution = await ApplyAction(action);
        Document configuration = Assert.Single(updatedSolution.GetProject(document.Project.Id)!.Documents,
            item => item.Name == "CSharpRewriter.Configuration.cs");
        string updatedText = (await configuration.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        output.Write(action, new ApplyChangesOperation(updatedSolution), updatedText);

        Assert.Contains("CSharpRewriterTypeMapping(\"__CSharpRewriterPlaceholder__.ReplaceMe\", \"Missing.Context\")", updatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnresolvableExtensionAddsPlaceholderExtensionMapping()
    {
        MockProject project = CreateProject("public class Host { }", out Document document);
        Diagnostic diagnostic = await CreateUnresolvableDiagnostic(document, "UnresolvableExtension", "Transform");

        ImmutableArray<CodeAction> actions = await project.GetCodeFixActionsAsync<CSharpRewriterConfigurationCodeFixProvider>(document, diagnostic);
        output.Write(actions);
        CodeAction action = Assert.Single(actions);
        Assert.Equal("Add placeholder extension mapping", action.Title);

        Solution updatedSolution = await ApplyAction(action);
        Document configuration = Assert.Single(updatedSolution.GetProject(document.Project.Id)!.Documents,
            item => item.Name == "CSharpRewriter.Configuration.cs");
        string updatedText = (await configuration.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        output.Write(action, new ApplyChangesOperation(updatedSolution), updatedText);

        Assert.Contains("CSharpRewriterTypeMapping(\"__CSharpRewriterPlaceholder__.ReplaceMe\", ExtensionMethods = new[] { \"Transform\" })", updatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BindingIssueDiagnosticAddsIgnoreUsingCompilerDiagnosticDetails()
    {
        const string host = "using Gori.CompressedStatics; public static partial class Sample { [CompressFile(\"payload.cs\", Transformer = \"CSharpRewriter\")] public static partial string Value { get; } }";
        const string payload = "public class Payload { public int Value = \"text\"; }";
        var project = new MockProject(typeof(CSharpRewriterIgnoreAttribute).Assembly,
            cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddAdditionalFile("src\\payload.cs", payload);
        Document document = project.AddDocument(host, "src\\Host.cs");
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(await project.GetCompilationAsync(), TestContext.Current.CancellationToken);
        GeneratorRunResult generatorResult = Assert.Single(driver.GetRunResult().Results);
        Diagnostic diagnostic = Assert.Single(generatorResult.Diagnostics,
            item => item.Id == DiagnosticDescriptors.CSharpRewriterBindingIssue.Id);
        Assert.Equal("CS0029", diagnostic.Properties["CompilerDiagnosticId"]);
        Assert.Contains("Cannot implicitly convert type", diagnostic.Properties["CompilerDiagnosticMessage"], StringComparison.Ordinal);

        ImmutableArray<CodeAction> actions = await project.GetCodeFixActionsAsync<CSharpRewriterConfigurationCodeFixProvider>(document, diagnostic);
        output.Write(actions);
        CodeAction action = Assert.Single(actions);
        Assert.Equal("Ignore compiler diagnostic CS0029 for cleanup", action.Title);
        Solution updatedSolution = await ApplyAction(action);
        Document configuration = Assert.Single(updatedSolution.GetProject(document.Project.Id)!.Documents,
            item => item.Name == "CSharpRewriter.Configuration.cs");
        string updatedText = (await configuration.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        output.Write(action, new ApplyChangesOperation(updatedSolution), updatedText);

        Assert.Contains("CSharpRewriterIgnore(\"CS0029\", \"Cannot implicitly convert type 'string' to 'int'\")", updatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnresolvableExtensionDiagnosticCreatesMappingForReportedMemberName()
    {
        const string host = "using Gori.CompressedStatics; public static partial class Sample { [CompressFile(\"payload.cs\", Transformer = \"CSharpRewriter\")] public static partial string Value { get; } }";
        const string payload = "public class Payload { public object Value(object item) => item.Transform(); }";
        var project = new MockProject(typeof(CSharpRewriterIgnoreAttribute).Assembly,
            cancellationToken: TestContext.Current.CancellationToken);
        _ = project.AddAdditionalFile("src\\payload.cs", payload);
        Document document = project.AddDocument(host, "src\\Host.cs");
        GeneratorDriver driver = project.CreateGeneratorDriver<CompressStaticsGenerator>()
            .RunGenerators(await project.GetCompilationAsync(), TestContext.Current.CancellationToken);
        GeneratorRunResult generatorResult = Assert.Single(driver.GetRunResult().Results);
        Diagnostic diagnostic = Assert.Single(generatorResult.Diagnostics,
            item => item.Id == DiagnosticDescriptors.CSharpRewriterUnresolvable.Id);
        Assert.Equal("UnresolvableExtension", diagnostic.Properties["UnresolvableType"]);
        Assert.Contains("Transform", diagnostic.Properties["UnresolvableName"], StringComparison.Ordinal);

        ImmutableArray<CodeAction> actions = await project.GetCodeFixActionsAsync<CSharpRewriterConfigurationCodeFixProvider>(document, diagnostic);
        output.Write(actions);
        CodeAction action = Assert.Single(actions);
        Solution updatedSolution = await ApplyAction(action);
        Document configuration = Assert.Single(updatedSolution.GetProject(document.Project.Id)!.Documents,
            item => item.Name == "CSharpRewriter.Configuration.cs");
        string updatedText = (await configuration.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        output.Write(action, new ApplyChangesOperation(updatedSolution), updatedText);

        Assert.Contains("ExtensionMethods = new[] { \"Transform\" }", updatedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotOfferAnotherIgnoreWhenExistingRuleCoversDiagnostic()
    {
        const string source = "using Gori.CompressedStatics; [assembly: CSharpRewriterIgnore(\"CS0029\")] public class Host { }";
        MockProject project = CreateProject(source, out Document document);
        Diagnostic diagnostic = await CreateBindingDiagnostic(document, "CS0029", "Cannot implicitly convert type 'string' to 'int'");

        ImmutableArray<CodeAction> actions = await project.GetCodeFixActionsAsync<CSharpRewriterConfigurationCodeFixProvider>(document, diagnostic);

        Assert.Empty(actions);
    }

    private static MockProject CreateProject(string source, out Document document)
    {
        var project = new MockProject(typeof(CSharpRewriterIgnoreAttribute).Assembly,
            cancellationToken: TestContext.Current.CancellationToken);
        document = project.AddDocument(source, "src\\Host.cs");
        return project;
    }

    private static async Task<Diagnostic> CreateBindingDiagnostic(Document document, string compilerId, string compilerMessage)
    {
        SyntaxTree syntaxTree = (await document.GetSyntaxTreeAsync(TestContext.Current.CancellationToken))!;
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
            .Add("CompilerDiagnosticId", compilerId)
            .Add("CompilerDiagnosticMessage", compilerMessage);
        return Diagnostic.Create(
            DiagnosticDescriptors.CSharpRewriterBindingIssue,
            Location.Create(syntaxTree, new TextSpan(0, 0)),
            properties: properties,
            messageArgs: ["Value", compilerId, compilerMessage, string.Empty]);
    }

    private static async Task<Diagnostic> CreateUnresolvableDiagnostic(Document document, string kind, string name)
    {
        SyntaxTree syntaxTree = (await document.GetSyntaxTreeAsync(TestContext.Current.CancellationToken))!;
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
            .Add("UnresolvableType", kind)
            .Add("UnresolvableName", name);
        return Diagnostic.Create(
            DiagnosticDescriptors.CSharpRewriterUnresolvable,
            Location.Create(syntaxTree, new TextSpan(0, 0)),
            properties: properties,
            messageArgs: ["Value", kind, name, string.Empty]);
    }

    private static async Task<Solution> ApplyAction(CodeAction action)
    {
        ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
        return Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution;
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
