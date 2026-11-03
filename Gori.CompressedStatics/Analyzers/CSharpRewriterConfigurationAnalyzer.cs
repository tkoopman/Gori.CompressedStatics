using System.Collections.Immutable;

using Gori.Common.Roslyn;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Gori.CompressedStatics.Analyzers;

/// <summary>Suggests improvements to assembly-wide CSharpRewriter configuration.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CSharpRewriterConfigurationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.CSharpRewriterBroadIgnore,
            DiagnosticDescriptors.CSharpRewriterMappingPlaceholder];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(static context =>
        {
            foreach (Diagnostic diagnostic in GetSuggestions(context.Compilation.Assembly, context.CancellationToken))
            {
                context.ReportDiagnostic(diagnostic);
            }
        });
    }

    private static IEnumerable<Diagnostic> GetSuggestions(ISymbol owner, CancellationToken cancellationToken)
    {
        foreach (AttributeData attribute in owner.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? name = attribute.AttributeClass?.ToDisplayString();
            if (name is not "Gori.CompressedStatics.CSharpRewriterIgnoreAttribute" and not "Gori.CompressedStatics.CSharpRewriterTypeMappingAttribute"
                || !attribute.TryGetConstructorArgument(0, out string? first))
            {
                continue;
            }

            Location location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None;
            if (name == "Gori.CompressedStatics.CSharpRewriterIgnoreAttribute")
            {
                string? messageFilter = attribute.GetConstructorArgumentAsString(1);
                if (messageFilter is null || messageFilter.IsWhiteSpace())
                {
                    yield return Diagnostic.Create(DiagnosticDescriptors.CSharpRewriterBroadIgnore, location, first);
                }

                continue;
            }

            if (!first.StartsWith(Const.PlaceholderNamespace + ".", StringComparison.Ordinal))
            {
                continue;
            }

            string key = attribute.GetConstructorArgumentAsString(1) ?? first.Split('.')[^1];
            yield return Diagnostic.Create(
                DiagnosticDescriptors.CSharpRewriterMappingPlaceholder,
                GetConstructorArgumentLocation(attribute, 0, cancellationToken),
                key);
        }
    }

    private static Location GetConstructorArgumentLocation(AttributeData attribute, int index, CancellationToken cancellationToken)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax { ArgumentList: { } arguments } syntax)
        {
            return Location.None;
        }

        string? parameterName = attribute.AttributeConstructor?.Parameters[index].Name;
        AttributeArgumentSyntax? argument = arguments.Arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == parameterName)
            ?? arguments.Arguments.Where(argument => argument.NameEquals is null).ElementAtOrDefault(index);
        return argument?.Expression.GetLocation() ?? syntax.GetLocation();
    }
}
