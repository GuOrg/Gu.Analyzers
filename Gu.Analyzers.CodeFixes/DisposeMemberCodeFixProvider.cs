namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeMemberCodeFixProvider))]
    [Shared]
    internal class DisposeMemberCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(GU0031DisposeMember.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

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
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var member = (MemberDeclarationSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                var memberSymbol = MemberSymbol(member, semanticModel, context.CancellationToken);
                IMethodSymbol disposeMethodSymbol;
                MethodDeclarationSyntax disposeMethodDeclaration;
                if (memberSymbol != null &&
                    memberSymbol.ContainingType.TryGetMethod("Dispose", out disposeMethodSymbol) &&
                    disposeMethodSymbol.Parameters.Length == 0 &&
                    disposeMethodSymbol.TryGetSingleDeclaration(context.CancellationToken, out disposeMethodDeclaration))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Dispose member.",
                            _ => ApplyFixAsync(context, syntaxRoot, disposeMethodDeclaration, memberSymbol),
                            nameof(DisposeMemberCodeFixProvider)),
                        diagnostic);
                }
            }
        }

        private static Task<Document> ApplyFixAsync(
            CodeFixContext context,
            SyntaxNode syntaxRoot,
            MethodDeclarationSyntax disposeMethod,
            ISymbol member)
        {
            var newDisposeStatement = CreateDisposeStatement(member);
            var statements = CreateStatements(disposeMethod, newDisposeStatement);
            if (disposeMethod.Body != null)
            {
                var updatedBody = disposeMethod.Body.WithStatements(statements);
                return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(disposeMethod.Body, updatedBody)));
            }

            if (disposeMethod.ExpressionBody != null)
            {
                var newMethod = disposeMethod.WithBody(SyntaxFactory.Block(statements))
                                             .WithExpressionBody(null)
                                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None));
                return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(disposeMethod, newMethod)));
            }

            return Task.FromResult(context.Document);
        }

        private static StatementSyntax CreateDisposeStatement(ISymbol member)
        {
            var prefix = member.Name[0] == '_' ? string.Empty : "this.";
            if (!Disposable.IsAssignableTo(MemberType(member)))
            {
                return SyntaxFactory.ParseStatement($"({prefix}{member.Name} as IDisposable)?.Dispose();")
                             .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                             .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            }

            var isReadonly = IsReadOnly(member);
            if (isReadonly)
            {
                return SyntaxFactory.ParseStatement($"{prefix}{member.Name}.Dispose();")
                                              .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                              .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            }

            return SyntaxFactory.ParseStatement($"{prefix}{member.Name}?.Dispose();")
                                .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
        }

        private static SyntaxList<StatementSyntax> CreateStatements(MethodDeclarationSyntax method, StatementSyntax newStatement)
        {
            if (method.ExpressionBody != null)
            {
                return SyntaxFactory.List(new[] { SyntaxFactory.ExpressionStatement(method.ExpressionBody.Expression), newStatement });
            }

            return method.Body.Statements.Add(newStatement);
        }

        private static bool IsReadOnly(ISymbol member)
        {
            var isReadOnly = (member as IFieldSymbol)?.IsReadOnly ?? (member as IPropertySymbol)?.IsReadOnly;
            if (isReadOnly == null)
            {
                throw new InvalidOperationException($"Could not figure out if member: {member} is readonly.");
            }

            return isReadOnly.Value;
        }

        private static ITypeSymbol MemberType(ISymbol member)
        {
            return (member as IFieldSymbol)?.Type ?? (member as IPropertySymbol)?.Type;
        }

        private static ISymbol MemberSymbol(MemberDeclarationSyntax member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var field = member as FieldDeclarationSyntax;
            if (field != null)
            {
                return semanticModel.GetDeclaredSymbol(field.Declaration.Variables.Single(), cancellationToken);
            }

            var property = member as PropertyDeclarationSyntax;
            if (property != null)
            {
                return semanticModel.GetDeclaredSymbol(property, cancellationToken);
            }

            throw new InvalidOperationException($"Could not fins symbol for: {member}");
        }
    }
}