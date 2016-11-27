namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveArgumentCodeFixProvider))]
    [Shared]
    internal class MoveArgumentCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0002NamedArgumentPositionMatches.DiagnosticId,
            GU0005ExceptionArgumentsPositions.DiagnosticId);

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
                if (string.IsNullOrEmpty(token.ValueText))
                {
                    continue;
                }

                if (diagnostic.Id == GU0002NamedArgumentPositionMatches.DiagnosticId)
                {
                    var arguments = (ArgumentListSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                    if (!HasWhitespaceTriviaOnly(arguments))
                    {
                        continue;
                    }

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Move arguments to match parameter positions.",
                            cancellationToken => ApplyFixGU0002Async(cancellationToken, context, semanticModel, arguments),
                            nameof(NameArgumentsCodeFixProvider)),
                        diagnostic);
                }

                if (diagnostic.Id == GU0005ExceptionArgumentsPositions.DiagnosticId)
                {
                    var argument = (ArgumentSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Move name argument to match parameter positions.",
                            cancellationToken => ApplyFixGU0005Async(cancellationToken, context, semanticModel, argument),
                            nameof(NameArgumentsCodeFixProvider)),
                        diagnostic);
                }
            }
        }

        private static Task<Document> ApplyFixGU0002Async(CancellationToken cancellationToken, CodeFixContext context, SemanticModel semanticModel, ArgumentListSyntax argumentListSyntax)
        {
            var arguments = new ArgumentSyntax[argumentListSyntax.Arguments.Count];
            var method = semanticModel.SemanticModelFor(argumentListSyntax.Parent)
                                      .GetSymbolInfo(argumentListSyntax.Parent, cancellationToken)
                                      .Symbol as IMethodSymbol;
            foreach (var argument in argumentListSyntax.Arguments)
            {
                var index = ParameterIndex(method, argument);
                var oldArg = argumentListSyntax.Arguments[index];
                arguments[index] = argument.WithLeadingTrivia(oldArg.GetLeadingTrivia())
                                           .WithTrailingTrivia(oldArg.GetTrailingTrivia());
            }

            var updated = argumentListSyntax.WithArguments(SyntaxFactory.SeparatedList(arguments, argumentListSyntax.Arguments.GetSeparators()));
            return Task.FromResult(context.Document.WithSyntaxRoot(semanticModel.SyntaxTree.GetRoot().ReplaceNode(argumentListSyntax, updated)));
        }

        private static Task<Document> ApplyFixGU0005Async(CancellationToken cancellationToken, CodeFixContext context, SemanticModel semanticModel, ArgumentSyntax nameArgument)
        {
            var argumentListSyntax = nameArgument.FirstAncestorOrSelf<ArgumentListSyntax>();
            var arguments = new ArgumentSyntax[argumentListSyntax.Arguments.Count];
            var method = semanticModel.SemanticModelFor(argumentListSyntax.Parent)
                                      .GetSymbolInfo(argumentListSyntax.Parent, cancellationToken)
                                      .Symbol as IMethodSymbol;
            var messageIndex = ParameterIndex(method, "message");
            var nameIndex = ParameterIndex(method, "paramName");
            for (var i = 0; i < argumentListSyntax.Arguments.Count; i++)
            {
                if (i == messageIndex)
                {
                    arguments[nameIndex] = argumentListSyntax.Arguments[i];
                    continue;
                }

                if (i == nameIndex)
                {
                    arguments[messageIndex] = argumentListSyntax.Arguments[i];
                    continue;
                }

                arguments[i] = argumentListSyntax.Arguments[i];
            }

            var updated = argumentListSyntax.WithArguments(SyntaxFactory.SeparatedList(arguments, argumentListSyntax.Arguments.GetSeparators()));
            return Task.FromResult(context.Document.WithSyntaxRoot(semanticModel.SyntaxTree.GetRoot().ReplaceNode(argumentListSyntax, updated)));
        }

        private static int ParameterIndex(IMethodSymbol method, ArgumentSyntax argument)
        {
            return ParameterIndex(method, argument.NameColon.Name.Identifier.ValueText);
        }

        private static int ParameterIndex(IMethodSymbol method, string name)
        {
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                if (method.Parameters[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool HasWhitespaceTriviaOnly(ArgumentListSyntax arguments)
        {
            foreach (var token in arguments.Arguments.GetWithSeparators())
            {
                if (!IsWhiteSpace(token.GetLeadingTrivia()) ||
                    !IsWhiteSpace(token.GetTrailingTrivia()))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsWhiteSpace(SyntaxTriviaList trivia)
        {
            foreach (var syntaxTrivia in trivia)
            {
                if (!(syntaxTrivia.IsKind(SyntaxKind.WhitespaceTrivia) ||
                      syntaxTrivia.IsKind(SyntaxKind.EndOfLineTrivia)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}