using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Gori.CompressedStatics.CSharp;

public sealed partial class CSharpRewriter
{
    private sealed partial class ScopedRewriter
    {
        /// <summary>
        /// Reports binding issues found in the semantic model. Filters out issues that are ignored by the rewriter
        /// configuration and adds unignored issues to the rewrite issues collection.
        /// </summary>
        /// <returns>true if an unignored error should prevent import cleanup; otherwise, false.</returns>
        public bool ReportBindingIssues()
        {
            bool blocksImports = false;
            foreach (Diagnostic diagnostic in semanticModel.GetDiagnostics(cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (diagnostic.Severity != DiagnosticSeverity.Error)
                {
                    continue;
                }

                TextSpan span = diagnostic.Location.SourceSpan;
                string message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
                Ignore[] matchingIgnores = [.. configuration.GetMatchingIgnores(diagnostic.Id, message)];
                MappingIgnoreOverlap[] overlaps = [.. handledReferences
                    .Where(reference => reference.Span.Contains(span) && !reference.TypeArguments.Any(argument => argument.Contains(span)))
                    .SelectMany(reference => matchingIgnores.Select(ignore =>
                        new MappingIgnoreOverlap(reference.Reference, reference.Destination, diagnostic.Id, ignore.MessageContains) { Location = diagnostic.Location }))];
                bool mapped = handledReferences.Any(reference => reference.Span.Contains(span)
                    && !reference.TypeArguments.Any(argument => argument.Contains(span)));
                if (diagnostic.Id is "CS0246" or "CS0234" or "CS0103" or "CS0104" or "CS1061" or "CS1929" or "CS0117" or "CS0121"
                    && mapped)
                {
                    issues.AddRange(overlaps);
                    continue;
                }

                if (matchingIgnores.Length > 0)
                {
                    continue;
                }

                blocksImports = true;
                UnresolvableType? kind = diagnostic.Id switch
                {
                    "CS0246" or "CS0234" or "CS0518" => UnresolvableType.UnresolvableType,
                    "CS0104" => UnresolvableType.AmbiguousType,
                    "CS1061" or "CS1929" => UnresolvableType.UnresolvableExtension,
                    _ => null,
                };
                issues.Add(kind is UnresolvableType type
                    ? new Unresolvable(type, semanticModel.SyntaxTree.GetText(cancellationToken).ToString(span)) { Location = diagnostic.Location }
                    : new BindingIssue(diagnostic.Id, message) { Location = diagnostic.Location });
            }

            return blocksImports;
        }

        // A mapped generic name does not excuse errors inside its type arguments.
        private void MarkHandled(SyntaxNode node, string destination)
            => handledReferences.Add((node.Span, [.. node.DescendantNodes().OfType<TypeArgumentListSyntax>().Select(arguments => arguments.Span)], node.ToString(), destination));
    }
}
