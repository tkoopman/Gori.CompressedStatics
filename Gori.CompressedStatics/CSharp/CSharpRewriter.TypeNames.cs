using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gori.CompressedStatics.CSharp;

public sealed partial class CSharpRewriter
{
    private sealed partial class ScopedRewriter
    {
        internal const string ResolvableImport = "CSharpRewriter.ResolvableImport";

        public override SyntaxNode? VisitAttribute(AttributeSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var type = semanticModel.GetTypeInfo(node, cancellationToken).Type as INamedTypeSymbol;
            AttributeSyntax visited;
            if (rewriter.RewriteTypeNames && !IsAliasQualified(node.Name))
            {
                NameSyntax? name = type is not null && !ContainsUnresolvedType(type)
                    ? SyntaxFactory.ParseName(GetFullyQualifiedName(type)).WithTriviaFrom(node.Name)
                    : MapName(node.Name, attribute: true);
                visited = name is not null
                    ? node.WithName(name).WithArgumentList((AttributeArgumentListSyntax?)Visit(node.ArgumentList))
                    : (AttributeSyntax)base.VisitAttribute(node)!;
            }
            else
            {
                visited = (AttributeSyntax)base.VisitAttribute(node)!;
            }

            return type is { TypeKind: not TypeKind.Error }
                ? visited.WithAdditionalAnnotations(new SyntaxAnnotation(Helper.ResolvedAttributeTypeAnnotation, type.ToDisplayString()))
                : visited;
        }

        public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return rewriter.RewriteTypeNames && node.Name is not null
                && semanticModel.GetSymbolInfo(node.Name, cancellationToken).Symbol is INamespaceSymbol namespaceSymbol
                    ? node.WithAdditionalAnnotations(new SyntaxAnnotation(ResolvableImport, namespaceSymbol.ToDisplayString()))
                    : node;
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!rewriter.RewriteTypeNames || IsAliasQualified(node))
            {
                return base.VisitMemberAccessExpression(node);
            }

            if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol namedType
                && !ContainsUnresolvedType(namedType))
            {
                return SyntaxFactory.ParseExpression(GetFullyQualifiedName(namedType)).WithTriviaFrom(node);
            }

            List<SimpleNameSyntax> segments = GetNameSegments(node.Expression);
            ISymbol? receiver = semanticModel.GetSymbolInfo(node.Expression, cancellationToken).Symbol;

            return segments.Count > 0
                && (receiver is null or IErrorTypeSymbol)
                && segments.All(segment => semanticModel.GetSymbolInfo(segment, cancellationToken).Symbol is null or INamespaceSymbol or INamedTypeSymbol)
                && MapName(node.Expression) is NameSyntax mapped
                    ? node.WithExpression(SyntaxFactory.ParseExpression(mapped.ToString())
                                                       .WithTriviaFrom(node.Expression))
                          .WithName((SimpleNameSyntax)base.Visit(node.Name)!)
                    : base.VisitMemberAccessExpression(node);
        }

        private static string GetFullyQualifiedName(INamedTypeSymbol namedType)
            => namedType.ToDisplayString(namedType.SpecialType is SpecialType.None
                ? Helper.FullyQualifiedTypeFormat
                : Helper.FullyQualifiedTypeFormatWithoutSpecialTypes);

        private static bool ContainsUnresolvedType(ITypeSymbol type)
            => type switch
            {
                IErrorTypeSymbol => true,
                IArrayTypeSymbol array => ContainsUnresolvedType(array.ElementType),
                IPointerTypeSymbol pointer => ContainsUnresolvedType(pointer.PointedAtType),
                INamedTypeSymbol named => named.TypeArguments.Any(ContainsUnresolvedType)
                    || (named.ContainingType is not null && ContainsUnresolvedType(named.ContainingType)),
                _ => false,
            };

        private static bool IsTypeContext(NameSyntax name)
            => name.Parent switch
            {
                VariableDeclarationSyntax declaration => declaration.Type == name,
                PropertyDeclarationSyntax declaration => declaration.Type == name,
                EventDeclarationSyntax declaration => declaration.Type == name,
                MethodDeclarationSyntax declaration => declaration.ReturnType == name,
                DelegateDeclarationSyntax declaration => declaration.ReturnType == name,
                ParameterSyntax declaration => declaration.Type == name,
                ObjectCreationExpressionSyntax expression => expression.Type == name,
                TypeOfExpressionSyntax expression => expression.Type == name,
                DefaultExpressionSyntax expression => expression.Type == name,
                CastExpressionSyntax expression => expression.Type == name,
                SizeOfExpressionSyntax expression => expression.Type == name,
                SimpleBaseTypeSyntax baseType => baseType.Type == name,
                TypeConstraintSyntax constraint => constraint.Type == name,
                TypeArgumentListSyntax => true,
                ArrayTypeSyntax => true,
                NullableTypeSyntax => true,
                PointerTypeSyntax => true,
                RefTypeSyntax => true,
                TupleElementSyntax => true,
                _ => name.Parent is TypeSyntax,
            };

        private static bool IsExpressionContext(NameSyntax name)
            => name.Parent switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression == name,
                ArgumentSyntax argument => argument.Expression == name,
                InvocationExpressionSyntax invocation => invocation.Expression == name,
                _ => false,
            };

        /// <summary>
        /// Rewrites a name using its resolved type or mapped name when applicable.
        /// </summary>
        /// <param name="name">The name syntax to rewrite.</param>
        /// <returns>The rewritten syntax node, or <c>null</c> if no replacement is produced.</returns>
        private SyntaxNode? RewriteName(NameSyntax name)
        {
            // Deconstruction var describes the whole designation, not a replaceable tuple type.
            if (name.IsVar && name.Parent is DeclarationExpressionSyntax { Designation: ParenthesizedVariableDesignationSyntax })
            {
                return name;
            }

            if (IsAliasQualified(name)
                || (name.Parent is QualifiedNameSyntax qualifiedName && qualifiedName.Right == name && IsAliasQualified(qualifiedName))
                || (name.Parent is AliasQualifiedNameSyntax aliasName && aliasName.Name == name && IsAliasQualified(aliasName))
                || (name.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == name))
            {
                return base.Visit(name);
            }

            ISymbol? symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol;
            var namedType = symbol as INamedTypeSymbol;
            if (namedType is null && IsTypeContext(name))
            {
                namedType = semanticModel.GetTypeInfo(name, cancellationToken).Type as INamedTypeSymbol;
            }

            if (namedType is null || namedType.IsAnonymousType || ContainsUnresolvedType(namedType))
            {
                if (namedType is { TypeKind: not TypeKind.Error, IsAnonymousType: false }
                    && name.Parent is not QualifiedNameSyntax)
                {
                    NameSyntax qualified = SyntaxFactory.ParseName(GetFullyQualifiedName(namedType));
                    TypeArgumentListSyntax[] originalArguments = [.. name.DescendantNodes().OfType<TypeArgumentListSyntax>()];
                    TypeArgumentListSyntax[] qualifiedArguments = [.. qualified.DescendantNodes().OfType<TypeArgumentListSyntax>()];
                    if (originalArguments.Length == qualifiedArguments.Length)
                    {
                        var arguments = qualifiedArguments.Select((argument, index) => (argument, index)).ToDictionary(pair => pair.argument, pair => pair.index);
                        return qualified.ReplaceNodes(qualifiedArguments, (argument, _) =>
                            (TypeArgumentListSyntax)Visit(originalArguments[arguments[argument]])!).WithTriviaFrom(name);
                    }
                }

                return (namedType is IErrorTypeSymbol || symbol is null)
                    && (IsTypeContext(name) || (name.Parent is MemberAccessExpressionSyntax { Expression: var expression } && expression == name))
                    && MapName(name) is NameSyntax mapped
                        ? mapped
                        : base.Visit(name);
            }

            string fullyQualifiedName = GetFullyQualifiedName(namedType);
            return IsExpressionContext(name)
                ? SyntaxFactory.ParseExpression(fullyQualifiedName).WithTriviaFrom(name)
                : SyntaxFactory.ParseTypeName(fullyQualifiedName).WithTriviaFrom(name);
        }

        private bool IsAliasQualified(SyntaxNode node)
            => node switch
            {
                IdentifierNameSyntax identifier => semanticModel.GetAliasInfo(identifier, cancellationToken) is not null,
                QualifiedNameSyntax qualified => IsAliasQualified(qualified.Left),
                AliasQualifiedNameSyntax qualified => IsAliasQualified(qualified.Alias),
                MemberAccessExpressionSyntax access => IsAliasQualified(access.Expression),
                _ => false,
            };
    }
}
