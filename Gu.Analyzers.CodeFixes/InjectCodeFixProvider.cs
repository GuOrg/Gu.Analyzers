namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InjectCodeFixProvider))]
    [Shared]
    internal class InjectCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0007PreferInjecting.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) || token.IsMissing)
                {
                    continue;
                }

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (node is ObjectCreationExpressionSyntax objectCreation)
                {
                    var type = (ITypeSymbol)semanticModel.GetSymbolSafe(objectCreation, context.CancellationToken)?.ContainingSymbol;
                    if (type == null)
                    {
                        continue;
                    }

                    var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName(type)))
                                                       .WithType(objectCreation.Type);
                    switch (GU0007PreferInjecting.CanInject(objectCreation, semanticModel, context.CancellationToken))
                    {
                        case GU0007PreferInjecting.Injectable.No:
                            continue;
                        case GU0007PreferInjecting.Injectable.Safe:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Inject",
                                    cancellationToken => ApplyFixAsync(context, syntaxRoot, objectCreation, parameterSyntax),
                                    nameof(InjectCodeFixProvider)),
                                diagnostic);
                            break;
                        case GU0007PreferInjecting.Injectable.Unsafe:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Inject UNSAFE",
                                    cancellationToken => ApplyFixAsync(context, syntaxRoot, objectCreation, parameterSyntax),
                                    nameof(InjectCodeFixProvider)),
                                diagnostic);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (node is IdentifierNameSyntax identifierName)
                {
                    var type = GU0007PreferInjecting.MemberType(semanticModel.GetSymbolSafe(identifierName, context.CancellationToken));
                    var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName(type)))
                                                       .WithType(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                    switch (GU0007PreferInjecting.IsInjectable(identifierName, semanticModel, context.CancellationToken))
                    {
                        case GU0007PreferInjecting.Injectable.No:
                            continue;
                        case GU0007PreferInjecting.Injectable.Safe:
                        case GU0007PreferInjecting.Injectable.Unsafe:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Inject UNSAFE",
                                    cancellationToken => ApplyFixAsync(context, syntaxRoot, identifierName, parameterSyntax),
                                    nameof(InjectCodeFixProvider)),
                                diagnostic);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private static Task<Document> ApplyFixAsync(CodeFixContext context, SyntaxNode syntaxRoot, ExpressionSyntax expression, ParameterSyntax parameterSyntax)
        {
            ExpressionSyntax OldNode(ExpressionSyntax e)
            {
                if (e is ObjectCreationExpressionSyntax)
                {
                    return e;
                }

                if (e.Parent is MemberAccessExpressionSyntax memberAccess)
                {
                    return OldNode(memberAccess);
                }

                return e;
            }

            if (GU0007PreferInjecting.TryGetSingleConstructor(expression, out ConstructorDeclarationSyntax ctor))
            {
                parameterSyntax = UniqueName(ctor.ParameterList, parameterSyntax);
                var oldNode = OldNode(expression);
                return Task.FromResult(context.Document.WithSyntaxRoot(new InjectRewriter(oldNode, parameterSyntax).Visit(syntaxRoot)));
            }

            return Task.FromResult(context.Document);
        }

        private static ParameterSyntax UniqueName(ParameterListSyntax parameterList, ParameterSyntax parameter)
        {
            if (parameterList != null)
            {
                foreach (var p in parameterList.Parameters)
                {
                    if (p.Identifier.ValueText == parameter.Identifier.ValueText)
                    {
                        return UniqueName(
                            parameterList,
                            parameter.WithIdentifier(SyntaxFactory.Identifier(parameter.Identifier.ValueText + "_")));
                    }
                }
            }

            return parameter;
        }

        private static string ParameterName(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType &&
                namedType.IsGenericType)
            {
                var definition = namedType.OriginalDefinition;
                if (definition?.TypeParameters.Length == 1)
                {
                    var parameter = definition.TypeParameters[0];
                    foreach (var constraintType in parameter.ConstraintTypes)
                    {
                        if (type.Name.Contains(constraintType.Name))
                        {
                            return type.Name.Replace(constraintType.Name, namedType.TypeArguments[0].Name)
                                       .FirstCharLower();
                        }
                    }
                }
            }

            return type.Name.FirstCharLower();
        }

        private class InjectRewriter : CSharpSyntaxRewriter
        {
            private readonly ExpressionSyntax oldNode;
            private readonly ParameterSyntax parameter;

            public InjectRewriter(ExpressionSyntax oldNode, ParameterSyntax parameter)
            {
                this.oldNode = oldNode;
                this.parameter = parameter;
            }

            public override SyntaxNode VisitParameterList(ParameterListSyntax node)
            {
                if (node.Parent is ConstructorDeclarationSyntax)
                {
                    return node.AddParameters(this.parameter);
                }

                return base.VisitParameterList(node);
            }

            public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if (node == this.oldNode)
                {
                    return SyntaxFactory.IdentifierName(this.parameter.Identifier);
                }

                return base.VisitObjectCreationExpression(node);
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (node == this.oldNode)
                {
                    return SyntaxFactory.IdentifierName(this.parameter.Identifier);
                }

                return base.VisitMemberAccessExpression(node);
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node == this.oldNode)
                {
                    return SyntaxFactory.IdentifierName(this.parameter.Identifier);
                }

                return base.VisitIdentifierName(node);
            }
        }
    }
}