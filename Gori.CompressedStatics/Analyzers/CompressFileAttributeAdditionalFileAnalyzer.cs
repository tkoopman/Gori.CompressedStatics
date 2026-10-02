using System.Collections.Immutable;

using Gori.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.CompressedStatics.Analyzers;

/// <summary>
/// Analyzes the usage of the Compressed attribute to ensure that the specified
/// file path corresponds to an additional file in the project.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CompressFileAttributeAdditionalFileAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [
            DiagnosticDescriptors.MSBuildProjectDirectoryNotVisible,
            DiagnosticDescriptors.SourceFileMustBeAdditionalFile,
        ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            AnalyzerConfigOptions globalOptions = compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

            _ = globalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out string? projectPath);

            compilationContext.RegisterSymbolAction(c => AnalyzeSymbol(c, projectPath), SymbolKind.Method, SymbolKind.Property);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, string? projectPath)
    {
        ISymbol symbol = context.Symbol;
        SyntaxNode? attributedDeclaration = GetAttributedDeclaration(symbol, context.CancellationToken);
        if (attributedDeclaration is null)
        {
            return;
        }

        string currentPath = Path.GetDirectoryName(attributedDeclaration.SyntaxTree.FilePath) ?? string.Empty;

        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (!AttributeHelper.IsCompressFileAttribute(attribute.AttributeClass?.ToDisplayString()))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length < 1
                || attribute.ConstructorArguments[0].Value is not string filePath
                || string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            bool relativeToProject = attribute.GetNamedArgumentAsBool(nameof(CompressFileAttribute.RelativeToProject));

            if (relativeToProject && string.IsNullOrWhiteSpace(projectPath))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MSBuildProjectDirectoryNotVisible,
                        attributedDeclaration.GetLocation()));
            }

            if (TryGetAbsoluteFilePath(currentPath, projectPath, filePath, relativeToProject, out string absoluteFilePath)
                && HasMatchingAdditionalFile(context.Options.AdditionalFiles, absoluteFilePath))
            {
                continue;
            }

            if (TryGetAbsoluteFilePath(currentPath, projectPath, filePath, !relativeToProject, out string alternateAbsoluteFilePath)
                && HasMatchingAdditionalFile(context.Options.AdditionalFiles, alternateAbsoluteFilePath)
                && TryGetRelativeFilePath(currentPath, projectPath, alternateAbsoluteFilePath, relativeToProject, out string relativeFilePath))
            {
                ImmutableDictionary<string, string?> properties
                    = ImmutableDictionary<string, string?>.Empty
                                                          .Add("RelativeToProjectAction", relativeToProject ? "Remove" : "Add")
                                                          .Add("RelativeFilePath", relativeFilePath);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.SourceFileMustBeAdditionalFile,
                        attributedDeclaration.GetLocation(),
                        properties,
                        filePath));
                continue;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.SourceFileMustBeAdditionalFile,
                    attributedDeclaration.GetLocation(),
                    filePath));
        }
    }

    private static bool TryGetAbsoluteFilePath(
        string currentPath,
        string? projectPath,
        string filePath,
        bool relativeToProject,
        out string absoluteFilePath)
    {
        absoluteFilePath = string.Empty;
        if (relativeToProject && string.IsNullOrWhiteSpace(projectPath))
        {
            return false;
        }

        try
        {
            absoluteFilePath = Path.GetFullPath(Path.Combine(relativeToProject ? projectPath : currentPath, filePath));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryGetRelativeFilePath(
        string currentPath,
        string? projectPath,
        string absoluteFilePath,
        bool relativeToProject,
        out string relativeFilePath)
    {
        relativeFilePath = string.Empty;
        if (relativeToProject && string.IsNullOrWhiteSpace(projectPath))
        {
            return false;
        }

        try
        {
            string basePath = Path.GetFullPath(relativeToProject ? projectPath! : currentPath);
            Uri baseUri = new(AppendDirectorySeparatorChar(basePath));
            Uri fileUri = new(Path.GetFullPath(absoluteFilePath));
            relativeFilePath = Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri)
                                                             .ToString()
                                                             .Replace('/', Path.DirectorySeparatorChar));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string AppendDirectorySeparatorChar(string path)
        => path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
        || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static bool HasMatchingAdditionalFile(ImmutableArray<AdditionalText> additionalFiles, string absoluteFilePath)
    {
        foreach (AdditionalText additionalFile in additionalFiles)
        {
            if (string.Equals(additionalFile.Path, absoluteFilePath, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the syntax node of the method or property declaration if it has a Compressed attribute applied to it.
    /// </summary>
    /// <param name="symbol">The symbol to check for the Compressed attribute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The syntax node of the method or property declaration if it has a Compressed attribute; otherwise, null.</returns>
    private static SyntaxNode? GetAttributedDeclaration(ISymbol symbol, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = syntaxReference.GetSyntax(cancellationToken);
            if (syntax is MethodDeclarationSyntax methodDeclaration
                && HasCompressFileAttribute(methodDeclaration.AttributeLists, cancellationToken))
            {
                return methodDeclaration;
            }

            if (syntax is PropertyDeclarationSyntax propertyDeclaration
                && HasCompressFileAttribute(propertyDeclaration.AttributeLists, cancellationToken))
            {
                return propertyDeclaration;
            }
        }

        return null;
    }

    private static bool HasCompressFileAttribute(SyntaxList<AttributeListSyntax> lists, CancellationToken cancellationToken)
    {
        foreach (AttributeListSyntax list in lists)
        {
            foreach (AttributeSyntax attribute in list.Attributes)
            {
                string name = attribute.Name.ToString();
                if (AttributeHelper.IsCompressFileAttribute(name))
                {
                    return true;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        return false;
    }
}
