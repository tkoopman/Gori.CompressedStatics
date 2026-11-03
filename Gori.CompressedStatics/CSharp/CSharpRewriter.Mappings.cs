using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gori.CompressedStatics.CSharp;

public sealed partial class CSharpRewriter
{
    private sealed partial class ScopedRewriter
    {
        private static List<SimpleNameSyntax> GetNameSegments(SyntaxNode name)
            => name switch
            {
                SimpleNameSyntax simple => [simple],
                QualifiedNameSyntax qualified => [.. GetNameSegments(qualified.Left), qualified.Right],
                MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                    && GetNameSegments(access.Expression) is { Count: > 0 } prefix => [.. prefix, access.Name],
                _ => [],
            };

        /// <summary>
        /// Applies a configured type mapping to a source name while preserving its type arguments and trailing name segments.
        /// </summary>
        /// <param name="name">The source type or attribute name to map.</param>
        /// <param name="attribute">Whether attribute-specific mapping rules should be considered.</param>
        /// <returns>The globally qualified mapped name, or <see langword="null"/> when no supported mapping applies.</returns>
        private NameSyntax? MapName(SyntaxNode name, bool attribute = false)
        {
            List<SimpleNameSyntax> segments = GetNameSegments(name);
            if (segments.Count == 0)
            {
                return null;
            }

            string[] identifiers = [.. segments.Select(segment => segment.Identifier.Text)];
            TypeMapping? mapping = configuration.FindTypeMapping(identifiers, attribute);
            if (mapping is null)
            {
                return null;
            }

            int prefixLength = mapping.Replaces.Split('.').Length;
            if (segments.Take(prefixLength - 1).Any(segment => segment is GenericNameSyntax))
            {
                if (unsupportedMappingReferences.Add(name))
                {
                    issues.Add(new InvalidConfiguration(InvalidConfigurationReason.UnsupportedContainingTypeArguments, name.ToString())
                    {
                        Location = name.GetLocation(),
                    });
                }

                return null;
            }

            SimpleNameSyntax sourceType = segments[prefixLength - 1];
            NameSyntax destination = SyntaxFactory.ParseName("global::" + mapping.FullTypeName);
            SimpleNameSyntax destinationType = destination is QualifiedNameSyntax qualified ? qualified.Right : ((AliasQualifiedNameSyntax)destination).Name;
            if (sourceType is GenericNameSyntax generic)
            {
                var arguments = (TypeArgumentListSyntax)Visit(generic.TypeArgumentList)!;
                SimpleNameSyntax replacement = SyntaxFactory.GenericName(destinationType.Identifier, arguments);
                destination = destination is QualifiedNameSyntax path ? path.WithRight(replacement) : ((AliasQualifiedNameSyntax)destination).WithName(replacement);
            }

            foreach (SimpleNameSyntax segment in segments.Skip(prefixLength))
            {
                destination = SyntaxFactory.QualifiedName(destination, (SimpleNameSyntax)base.Visit(segment)!);
            }

            MarkHandled(name, destination.ToString());
            return destination.WithTriviaFrom(name);
        }
    }
}
