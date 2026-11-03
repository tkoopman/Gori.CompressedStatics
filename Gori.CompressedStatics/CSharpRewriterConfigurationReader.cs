#nullable enable

using System.Collections.Immutable;

using Gori.Common.Roslyn;
using Gori.CompressedStatics.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gori.CompressedStatics;

/// <summary>Loads transformer configuration from host assembly attributes.</summary>
internal static class CSharpRewriterConfigurationReader
{
    /// <summary>Validates assembly rules while retaining their diagnostic source locations.</summary>
    /// <param name="compilation">The original host compilation.</param>
    /// <param name="issues">Invalid values and conflicting mappings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reusable programmatic configuration.</returns>
    internal static RewriterConfiguration Load(
        Compilation compilation,
        out IEnumerable<RewriteIssue> issues,
        CancellationToken cancellationToken)
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        cancellationToken.ThrowIfCancellationRequested();
        List<RewriteIssue>? issueList = null;
        List<Ignore>? ignores = null;
        List<(TypeMapping Mapping, Location? Location)>? mappings = null;
        foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? name = attribute.AttributeClass?.ToDisplayString();
            if (name is not "Gori.CompressedStatics.CSharpRewriterIgnoreAttribute" and not "Gori.CompressedStatics.CSharpRewriterTypeMappingAttribute")
            {
                continue;
            }

            SyntaxNode? syntax = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
            Location? location = syntax?.GetLocation();
            bool isIgnore = name == "Gori.CompressedStatics.CSharpRewriterIgnoreAttribute";
            string firstName = isIgnore ? "diagnosticID" : "fullTypeName";
            string secondName = isIgnore ? "messageContains" : "replaces";
            if (!attribute.TryGetConstructorArgument(0, out string? first))
            {
                AddInvalidValue(
                    "A non-null string configuration value is required.",
                    argumentName: firstName,
                    argumentValue: attribute.GetConstructorArgumentAsString(0));
                continue;
            }

            string? second = null;
            if (AttributeDataExtensions.TryGetConstructorArgument(attribute, 1, out TypedConstant secondArgument))
            {
                if (secondArgument.Kind == TypedConstantKind.Error
                    || (!secondArgument.IsNull && !attribute.TryGetConstructorArgument(1, out second)))
                {
                    AddInvalidValue(
                        "A string configuration value is required.",
                        argumentName: secondName,
                        argumentValue: attribute.GetConstructorArgumentAsString(1));
                    continue;
                }
            }

            if (isIgnore)
            {
                if (first.Length < 3 || !first.StartsWith("CS", StringComparison.Ordinal)
                    || !first.Skip(2).All(character => character is >= '0' and <= '9'))
                {
                    AddInvalidValue(
                        "DiagnosticID must be a compiler diagnostic ID such as CS0656.",
                        argumentName: firstName,
                        argumentValue: first);
                    continue;
                }

                (ignores ??= []).Add(new Ignore(first, second));
                continue;
            }

            // Silently ignore placeholder mappings. Diagnostics for these are reported by the analyzer, not the rewriter.
            if (first.StartsWith(Const.PlaceholderNamespace + ".", StringComparison.Ordinal))
            {
                continue;
            }

            ImmutableArray<ExtensionDefinition> extensions = [];
            if (attribute.TryGetNamedArgument(nameof(CSharpRewriterTypeMappingAttribute.ExtensionMethods), out TypedConstant extensionArgument)
                || attribute.NamedArguments.Any(argument => argument.Key == nameof(CSharpRewriterTypeMappingAttribute.ExtensionMethods)))
            {
                if (extensionArgument.IsNull || extensionArgument.Kind != TypedConstantKind.Array)
                {
                    AddInvalidValue(
                        "ExtensionMethods must be a non-null array.",
                        argumentName: nameof(CSharpRewriterTypeMappingAttribute.ExtensionMethods),
                        argumentValue: extensionArgument.IsNull ? null : extensionArgument.ToCSharpString());
                    continue;
                }

                ImmutableArray<ExtensionDefinition>.Builder definitions = ImmutableArray.CreateBuilder<ExtensionDefinition>();
                bool invalidElement = false;
                for (int index = 0; index < extensionArgument.Values.Length; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TypedConstant value = extensionArgument.Values[index];
                    string? argumentValue = value.Kind == TypedConstantKind.Error ? null : value.Value as string;
                    if (!ExtensionDefinition.TryCreate(argumentValue, out ExtensionDefinition? definition, out InvalidConfiguration? extensionIssue))
                    {
                        AddInvalidValue(
                            extensionIssue.Message!,
                            argumentName: nameof(CSharpRewriterTypeMappingAttribute.ExtensionMethods),
                            argumentValue: argumentValue,
                            argumentIndex: index);
                        invalidElement = true;
                        break;
                    }

                    definitions.Add(definition);
                }

                if (invalidElement)
                {
                    continue;
                }

                extensions = definitions.ToImmutable();
            }

            if (!TypeMapping.TryCreate(first, second, out TypeMapping? mapping, out InvalidConfiguration? mappingIssue, extensions))
            {
                (issueList ??= []).Add(mappingIssue with { Value = syntax?.ToString(), Location = location });
                continue;
            }

            (mappings ??= []).Add((mapping, location));

            void AddInvalidValue(string message, string? argumentName = null, int? argumentIndex = null, string? argumentValue = null)
                => (issueList ??= []).Add(new InvalidConfiguration(InvalidConfigurationReason.InvalidValue, syntax?.ToString())
                {
                    Location = location,
                    Message = message,
                    ArgumentName = argumentName,
                    ArgumentIndex = argumentIndex,
                    ArgumentValue = argumentValue,
                });
        }

        var configuration = RewriterConfiguration.CreateWithLocations(
            ignores?.ToImmutableArray() ?? [], mappings ?? [], out IEnumerable<RewriteIssue> mappingIssues, cancellationToken);
        issues = issueList is null ? mappingIssues : [.. issueList, .. mappingIssues];
        return configuration;
    }
}
