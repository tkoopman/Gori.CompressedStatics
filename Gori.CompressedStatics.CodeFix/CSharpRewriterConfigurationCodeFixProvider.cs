using System.Collections.Immutable;
using System.Composition;

using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.CodeFix;

/// <summary>Creates or updates the assembly-wide CSharpRewriter configuration file.</summary>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpRewriterConfigurationCodeFixProvider))]
public sealed class CSharpRewriterConfigurationCodeFixProvider : CodeFixProvider
{
    private const string ConfigurationFileName = "CSharpRewriter.Configuration.cs";
    private const string PlaceholderDestination = Const.PlaceholderNamespace + ".ReplaceMe";
    private const string CompilerDiagnosticIdProperty = "CompilerDiagnosticId";
    private const string CompilerDiagnosticMessageProperty = "CompilerDiagnosticMessage";
    private const string UnresolvableTypeProperty = "UnresolvableType";
    private const string UnresolvableNameProperty = "UnresolvableName";
    private const string IgnoreAttributeName = "Gori.CompressedStatics.CSharpRewriterIgnoreAttribute";
    private const string TypeMappingAttributeName = "Gori.CompressedStatics.CSharpRewriterTypeMappingAttribute";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds
        => [
            DiagnosticDescriptors.CSharpRewriterBindingIssue.Id,
            DiagnosticDescriptors.CSharpRewriterUnresolvable.Id,
        ];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Compilation? compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            return;
        }

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id == DiagnosticDescriptors.CSharpRewriterBindingIssue.Id)
            {
                RegisterIgnoreFix(context, diagnostic, compilation);
            }
            else if (diagnostic.Id == DiagnosticDescriptors.CSharpRewriterUnresolvable.Id)
            {
                RegisterMappingFix(context, diagnostic, compilation);
            }
        }
    }

    private static void RegisterIgnoreFix(CodeFixContext context, Diagnostic diagnostic, Compilation compilation)
    {
        if (!diagnostic.Properties.TryGetValue(CompilerDiagnosticIdProperty, out string? compilerDiagnosticId)
            || compilerDiagnosticId is null || compilerDiagnosticId.IsWhiteSpace()
            || !diagnostic.Properties.TryGetValue(CompilerDiagnosticMessageProperty, out string? compilerDiagnosticMessage)
            || compilerDiagnosticMessage is null || compilerDiagnosticMessage.IsWhiteSpace())
        {
            return;
        }

        Ignore? polyfillMatch = Ignore.PolyfillIgnores.FirstOrDefault(ignore =>
            StringComparer.Ordinal.Equals(ignore.DiagnosticID, compilerDiagnosticId)
            && ignore.MessageContains is not null
            && compilerDiagnosticMessage.Contains(ignore.MessageContains, StringComparison.Ordinal));

        if (polyfillMatch is not null)
        {
            Ignore[] missingIgnores = [.. Ignore.PolyfillIgnores.Where(required =>
                !IsCoveredByExistingIgnore(compilation, required))];
            if (missingIgnores.Length == 0)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add all required polyfill ignores",
                    createChangedSolution: cancellationToken => AddConfigurationAsync(
                        context.Document.Project,
                        [.. missingIgnores.Select(CreateIgnoreAttribute)],
                        cancellationToken),
                    equivalenceKey: "CSharpRewriter.AddPolyfillIgnores"),
                diagnostic);
            return;
        }

        var target = new Ignore(compilerDiagnosticId, compilerDiagnosticMessage);
        if (IsCoveredByExistingIgnore(compilation, target))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Ignore compiler diagnostic {compilerDiagnosticId} for cleanup",
                createChangedSolution: cancellationToken => AddConfigurationAsync(
                    context.Document.Project,
                    [CreateIgnoreAttribute(target)],
                    cancellationToken),
                equivalenceKey: "CSharpRewriter.AddIgnore"),
            diagnostic);
    }

    private static void RegisterMappingFix(CodeFixContext context, Diagnostic diagnostic, Compilation compilation)
    {
        if (!diagnostic.Properties.TryGetValue(UnresolvableTypeProperty, out string? unresolvedType)
            || !diagnostic.Properties.TryGetValue(UnresolvableNameProperty, out string? unresolvedName)
            || unresolvedName is null || unresolvedName.IsWhiteSpace())
        {
            return;
        }

        if (StringComparer.Ordinal.Equals(unresolvedType, UnresolvableType.UnresolvableExtension.ToString()))
        {
            string? methodName = GetExtensionMethodName(unresolvedName);
            if (methodName is null || HasPlaceholderExtensionMapping(compilation, methodName))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add placeholder extension mapping",
                    createChangedSolution: cancellationToken => AddConfigurationAsync(
                        context.Document.Project,
                        [CreateExtensionMappingAttribute(methodName)],
                        cancellationToken),
                    equivalenceKey: "CSharpRewriter.AddPlaceholderExtensionMapping"),
                diagnostic);
            return;
        }

        if (!StringComparer.Ordinal.Equals(unresolvedType, UnresolvableType.UnresolvableType.ToString())
            || !TryNormalizeTypeName(unresolvedName, out string? sourceType)
            || HasPlaceholderTypeMapping(compilation, sourceType))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add placeholder type mapping",
                createChangedSolution: cancellationToken => AddConfigurationAsync(
                    context.Document.Project,
                    [CreateTypeMappingAttribute(sourceType)],
                    cancellationToken),
                equivalenceKey: "CSharpRewriter.AddPlaceholderTypeMapping"),
            diagnostic);
    }

    private static async Task<Solution> AddConfigurationAsync(Project project, ImmutableArray<string> attributes, CancellationToken cancellationToken)
    {
        Document? configurationDocument = FindConfigurationDocument(project);
        if (configurationDocument is null)
        {
            const string newline = "\n";
            string content = "using Gori.CompressedStatics;" + newline + newline
                + string.Join(newline, attributes) + newline;
            string? projectDirectory = project.FilePath is null ? null : Path.GetDirectoryName(project.FilePath);
            string? filePath = projectDirectory is null ? null : Path.Combine(projectDirectory, ConfigurationFileName);
            var documentId = DocumentId.CreateNewId(project.Id, ConfigurationFileName);
            return project.Solution.AddDocument(
                documentId,
                ConfigurationFileName,
                SourceText.From(content),
                filePath: filePath);
        }

        SyntaxNode? root = await configurationDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            throw new InvalidOperationException($"Unable to update {ConfigurationFileName} because it has no C# compilation unit.");
        }

        CompilationUnitSyntax additions = SyntaxFactory.ParseCompilationUnit(string.Join("\n", attributes));
        CompilationUnitSyntax updatedRoot = compilationUnit;
        if (!updatedRoot.Usings.Any(usingDirective => usingDirective.Name?.ToString() == "Gori.CompressedStatics"))
        {
            updatedRoot = updatedRoot.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Gori.CompressedStatics"))
                    .WithAdditionalAnnotations(Formatter.Annotation));
        }

        updatedRoot = updatedRoot.AddAttributeLists([.. additions.AttributeLists.Select(attributeList => attributeList.WithAdditionalAnnotations(Formatter.Annotation))]);
        Document updatedDocument = configurationDocument.WithSyntaxRoot(updatedRoot);
        Document formattedDocument = await Formatter.FormatAsync(
            updatedDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        return formattedDocument.Project.Solution;
    }

    private static Document? FindConfigurationDocument(Project project)
    {
        string? projectDirectory = project.FilePath is null ? null : Path.GetDirectoryName(project.FilePath);
        return project.Documents.FirstOrDefault(document
            => StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(document.FilePath ?? document.Name), ConfigurationFileName)
            && (projectDirectory is null
                || document.FilePath is null
                || StringComparer.OrdinalIgnoreCase.Equals(
                    Path.GetFullPath(Path.GetDirectoryName(document.FilePath) ?? projectDirectory),
                    Path.GetFullPath(projectDirectory))));
    }

    private static bool IsCoveredByExistingIgnore(Compilation compilation, Ignore required)
        => GetExistingIgnores(compilation).Any(existing =>
            StringComparer.Ordinal.Equals(existing.DiagnosticID, required.DiagnosticID)
            && (existing.MessageContains is null
                || (required.MessageContains is not null
                    && required.MessageContains.Contains(existing.MessageContains, StringComparison.Ordinal))));

    private static IEnumerable<Ignore> GetExistingIgnores(Compilation compilation)
    {
        foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != IgnoreAttributeName
                || attribute.ConstructorArguments.Length == 0
                || attribute.ConstructorArguments[0].Value is not string diagnosticId)
            {
                continue;
            }

            string? messageContains = attribute.ConstructorArguments.Length > 1
                ? attribute.ConstructorArguments[1].Value as string
                : null;
            yield return new Ignore(diagnosticId, messageContains);
        }
    }

    private static bool HasPlaceholderTypeMapping(Compilation compilation, string sourceType)
        => GetTypeMappingAttributes(compilation).Any(attribute =>
            IsPlaceholderDestination(attribute)
            && StringComparer.Ordinal.Equals(GetMappingSource(attribute), sourceType));

    private static bool HasPlaceholderExtensionMapping(Compilation compilation, string methodName)
        => GetTypeMappingAttributes(compilation).Any(attribute =>
            IsPlaceholderDestination(attribute)
            && attribute.NamedArguments.Any(argument => argument.Key == nameof(CSharpRewriterTypeMappingAttribute.ExtensionMethods)
                && argument.Value.Kind == TypedConstantKind.Array
                && argument.Value.Values.Any(value => StringComparer.Ordinal.Equals(value.Value as string, methodName))));

    private static IEnumerable<AttributeData> GetTypeMappingAttributes(Compilation compilation)
        => compilation.Assembly.GetAttributes().Where(attribute => attribute.AttributeClass?.ToDisplayString() == TypeMappingAttributeName);

    private static bool IsPlaceholderDestination(AttributeData attribute)
        => attribute.ConstructorArguments.Length > 0
            && attribute.ConstructorArguments[0].Value is string destination
            && destination.StartsWith(Const.PlaceholderNamespace + ".", StringComparison.Ordinal);

    private static string? GetMappingSource(AttributeData attribute)
        => attribute.ConstructorArguments.Length > 1
        && attribute.ConstructorArguments[1].Value is string source
            ? source
            : attribute.ConstructorArguments.FirstOrDefault().Value is string destination
                ? destination.Split('.').Last()
                : null;

    private static bool TryNormalizeTypeName(string value, out string typeName)
    {
        typeName = value.Trim();
        if (typeName.StartsWith("global::", StringComparison.Ordinal))
        {
            typeName = typeName["global::".Length..];
        }

        return TypeMapping.TryCreate(PlaceholderDestination, typeName, out _, out _);
    }

    private static string? GetExtensionMethodName(string unresolvedName)
    {
        ExpressionSyntax expression = SyntaxFactory.ParseExpression(unresolvedName);
        while (expression is InvocationExpressionSyntax invocation)
        {
            expression = invocation.Expression;
        }

        SimpleNameSyntax? method = expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            SimpleNameSyntax simpleName => simpleName,
            _ => null,
        };

        return method is null || method.ContainsDiagnostics || method.Identifier.IsMissing ? null : method.Identifier.ValueText;
    }

    private static string CreateIgnoreAttribute(Ignore ignore)
        => ignore.MessageContains is null
            ? $"[assembly: CSharpRewriterIgnore({SymbolDisplay.FormatLiteral(ignore.DiagnosticID, quote: true)})]"
            : $"[assembly: CSharpRewriterIgnore({SymbolDisplay.FormatLiteral(ignore.DiagnosticID, quote: true)}, {SymbolDisplay.FormatLiteral(ignore.MessageContains, quote: true)})]";

    private static string CreateTypeMappingAttribute(string sourceType)
        => $"[assembly: CSharpRewriterTypeMapping({SymbolDisplay.FormatLiteral(PlaceholderDestination, quote: true)}, {SymbolDisplay.FormatLiteral(sourceType, quote: true)})]";

    private static string CreateExtensionMappingAttribute(string methodName)
        => $"[assembly: CSharpRewriterTypeMapping({SymbolDisplay.FormatLiteral(PlaceholderDestination, quote: true)}, ExtensionMethods = new[] {{ {SymbolDisplay.FormatLiteral(methodName, quote: true)} }})]";
}
