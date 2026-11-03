using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gori.CompressedStatics.CSharp;

public sealed partial class CSharpRewriter
{
    private sealed partial class ScopedRewriter
    {
        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
            if (!rewriter.RewriteExtensionCalls
                || node.Expression is not MemberAccessExpressionSyntax
                || visited.Expression is not MemberAccessExpressionSyntax memberAccess
                || node.ContainsDirectives
                || node.Ancestors().Any(ancestor => ancestor is ConditionalAccessExpressionSyntax or QueryExpressionSyntax))
            {
                return visited;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
            if (symbolInfo.Symbol is null)
            {
                return RewriteMappedExtension(node, visited, memberAccess);
            }

            if (symbolInfo.Symbol is not IMethodSymbol method
                || method.ReducedFrom is not IMethodSymbol extensionMethod
                || method.TypeArguments.Any(type => !CanNameType(type)))
            {
                return visited;
            }

            SimpleNameSyntax methodName = memberAccess.Name;
            if (method.IsGenericMethod && methodName is not GenericNameSyntax)
            {
                methodName = SyntaxFactory.GenericName(
                    memberAccess.Name.Identifier,
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(method.TypeArguments.Select(type =>
                        SyntaxFactory.ParseTypeName(type.ToDisplayString(Helper.FullyQualifiedTypeFormat))))));
            }

            MemberAccessExpressionSyntax target = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseExpression(extensionMethod.ContainingType.ToDisplayString(Helper.FullyQualifiedTypeFormat)),
                    methodName).WithOperatorToken(memberAccess.OperatorToken);
            ArgumentSyntax receiver = SyntaxFactory.Argument(memberAccess.Expression.WithoutLeadingTrivia());
            if (extensionMethod.Parameters[0].RefKind == RefKind.Ref)
            {
                receiver = receiver.WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword).WithTrailingTrivia(SyntaxFactory.Space));
            }

            InvocationExpressionSyntax rewritten = visited
                .WithExpression(target)
                .WithArgumentList(visited.ArgumentList.WithArguments(visited.ArgumentList.Arguments.Insert(0, receiver)))
                .WithTriviaFrom(node);

            // Verify binding against the original children: rewritten children may refer to types declared in this file.
            var originalAccess = (MemberAccessExpressionSyntax)node.Expression;
            InvocationExpressionSyntax bindingCandidate = rewritten.WithArgumentList(node.ArgumentList.WithArguments(
                node.ArgumentList.Arguments.Insert(0, receiver.WithExpression(originalAccess.Expression.WithoutLeadingTrivia()))));
            return semanticModel.GetSpeculativeSymbolInfo(node.SpanStart, bindingCandidate, SpeculativeBindingOption.BindAsExpression).Symbol is not IMethodSymbol bound
                || !SymbolEqualityComparer.Default.Equals(bound.OriginalDefinition, extensionMethod.OriginalDefinition)
                || !Enumerable.SequenceEqual(bound.TypeArguments, method.TypeArguments, SymbolEqualityComparer.Default)
                ? visited
                : rewritten;
        }

        private static bool CanNameType(ITypeSymbol type)
            => type switch
            {
                IErrorTypeSymbol => false,
                IArrayTypeSymbol array => CanNameType(array.ElementType),
                IPointerTypeSymbol pointer => CanNameType(pointer.PointedAtType),
                INamedTypeSymbol named => !named.IsAnonymousType && named.TypeArguments.All(CanNameType)
                    && (named.ContainingType is null || CanNameType(named.ContainingType)),
                _ => true,
            };

        /// <summary>
        /// Compares parameter types recursively, preserving tuple structure and nullable distinctions.
        /// </summary>
        /// <param name="actual">The type inferred from the source argument.</param>
        /// <param name="expected">The type required by the configured extension mapping.</param>
        /// <returns><see langword="true"/> when the types match including nullability; otherwise, <see langword="false"/>.</returns>
        private static bool ParameterTypesEqual(ITypeSymbol actual, ITypeSymbol expected)
            => (actual, expected) switch
            {
                (INamedTypeSymbol { IsTupleType: true } left, INamedTypeSymbol { IsTupleType: true } right) =>
                    left.TupleElements.Length == right.TupleElements.Length
                    && left.TupleElements.Zip(right.TupleElements, (first, second) => ParameterTypesEqual(first.Type, second.Type)).All(equal => equal),
                (IArrayTypeSymbol left, IArrayTypeSymbol right) =>
                    left.Rank == right.Rank && left.NullableAnnotation == right.NullableAnnotation
                    && ParameterTypesEqual(left.ElementType, right.ElementType),
                (INamedTypeSymbol left, INamedTypeSymbol right) =>
                    SymbolEqualityComparer.IncludeNullability.Equals(left.OriginalDefinition, right.OriginalDefinition)
                    && left.NullableAnnotation == right.NullableAnnotation
                    && left.TypeArguments.Length == right.TypeArguments.Length
                    && left.TypeArguments.Zip(right.TypeArguments, ParameterTypesEqual).All(equal => equal)
                    && (left.ContainingType is null || ParameterTypesEqual(left.ContainingType, right.ContainingType!)),
                _ => SymbolEqualityComparer.IncludeNullability.Equals(actual, expected),
            };

        /// <summary>
        /// Rewrites an extension invocation using a configured type mapping when its receiver and arguments can be matched safely.
        /// </summary>
        /// <param name="original">The invocation in the original source.</param>
        /// <param name="visited">The invocation after visiting its child nodes.</param>
        /// <param name="memberAccess">The visited member access used for the call.</param>
        /// <returns>The mapped static call when a unique safe mapping applies; otherwise, the visited invocation.</returns>
        private InvocationExpressionSyntax RewriteMappedExtension(
            InvocationExpressionSyntax original,
            InvocationExpressionSyntax visited,
            MemberAccessExpressionSyntax memberAccess)
        {
            var originalAccess = (MemberAccessExpressionSyntax)original.Expression;
            if (original.ArgumentList.Arguments.Any(argument => argument.NameColon is not null || argument.RefKindKeyword.RawKind != 0)
                || semanticModel.GetTypeInfo(originalAccess.Expression, cancellationToken).Type?.TypeKind == TypeKind.Dynamic
                || semanticModel.GetSymbolInfo(originalAccess.Expression, cancellationToken).Symbol is INamedTypeSymbol or INamespaceSymbol)
            {
                return visited;
            }

            ExpressionSyntax[] arguments = [originalAccess.Expression, .. original.ArgumentList.Arguments.Select(argument => argument.Expression)];
            TypeMapping[] matches = [.. configuration.TypeMappings.Values.Where(mapping => !mapping.ExtensionMethods.IsDefaultOrEmpty
                && mapping.ExtensionMethods.Any(definition =>
                    definition.Name == originalAccess.Name.Identifier.Text
                    && (definition.Parameters.IsDefaultOrEmpty
                        ? definition.AllowExtra
                        : (definition.AllowExtra ? arguments.Length >= definition.Parameters.Length : arguments.Length == definition.Parameters.Length)
                            && definition.Parameters.Select((pattern, index) => MatchesParameter(pattern, arguments[index])).All(match => match))))];

            if (matches.Length > 1)
            {
                issues.Add(new InvalidConfiguration(InvalidConfigurationReason.ConflictingRules, originalAccess.Name.ToString()) { Location = originalAccess.Name.GetLocation() });
                return visited;
            }

            if (matches.Length == 0)
            {
                return visited;
            }

            MemberAccessExpressionSyntax target = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParseExpression("global::" + matches[0].FullTypeName),
                memberAccess.Name);
            MarkHandled(originalAccess.Name, target.ToString());
            ArgumentSyntax receiver = SyntaxFactory.Argument(memberAccess.Expression.WithoutLeadingTrivia());
            return visited.WithExpression(target)
                .WithArgumentList(visited.ArgumentList.WithArguments(visited.ArgumentList.Arguments.Insert(0, receiver)))
                .WithTriviaFrom(original);
        }

        private bool MatchesParameter(string pattern, ExpressionSyntax argument)
        {
            if (pattern == "*")
            {
                return true;
            }

            ITypeSymbol? actual = semanticModel.GetTypeInfo(argument, cancellationToken).Type;
            ITypeSymbol? expected = semanticModel.GetSpeculativeTypeInfo(
                argument.SpanStart,
                SyntaxFactory.ParseTypeName(pattern),
                SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
            return actual is not null && expected is not null && CanNameType(actual) && CanNameType(expected)
                && ParameterTypesEqual(actual, expected);
        }
    }
}
