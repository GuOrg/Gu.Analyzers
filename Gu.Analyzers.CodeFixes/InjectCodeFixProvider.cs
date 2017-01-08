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
                var objectCreation = node as ObjectCreationExpressionSyntax;
                if (objectCreation != null)
                {
                    var type = (ITypeSymbol)semanticModel.GetSymbolSafe(objectCreation, context.CancellationToken)?.ContainingSymbol;
                    if (type == null)
                    {
                        continue;
                    }

                    var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(Parametername(type)))
                                                       .WithType(SyntaxFactory.ParseTypeName(type.Name));
                    switch (GU0007PreferInjecting.CanInject(objectCreation, objectCreation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()))
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

                var memberAccess = node as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    var type = GU0007PreferInjecting.MemberType(semanticModel.GetSymbolSafe(memberAccess, context.CancellationToken));
                    var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(Parametername(type)))
                                                       .WithType(SyntaxFactory.ParseTypeName(type.Name));
                    switch (GU0007PreferInjecting.IsInjectable(memberAccess, memberAccess.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()))
                    {
                        case GU0007PreferInjecting.Injectable.No:
                            continue;
                        case GU0007PreferInjecting.Injectable.Safe:
                        case GU0007PreferInjecting.Injectable.Unsafe:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Inject UNSAFE",
                                    cancellationToken => ApplyFixAsync(context, syntaxRoot, memberAccess, parameterSyntax),
                                    nameof(InjectCodeFixProvider)),
                                diagnostic);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private static Task<Document> ApplyFixAsync(CodeFixContext context, SyntaxNode syntaxRoot, ExpressionSyntax objectCreation, ParameterSyntax parameterSyntax)
        {
            var ctor = objectCreation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            var updated = ctor.ReplaceNode(objectCreation, SyntaxFactory.IdentifierName(parameterSyntax.Identifier));
            updated = updated.WithParameterList(ctor.ParameterList.AddParameters(parameterSyntax));
            return Task.FromResult(context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(ctor, updated)));
        }

        private static string Parametername(ITypeSymbol type)
        {
            var typeName = type.Name;
            if (char.IsUpper(typeName[0]))
            {
                return new string(char.ToLower(typeName[0]), 1) + typeName.Substring(1);
            }

            return typeName;
        }
    }
}