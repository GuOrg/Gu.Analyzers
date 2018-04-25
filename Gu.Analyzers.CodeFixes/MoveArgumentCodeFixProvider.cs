namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveArgumentCodeFixProvider))]
    [Shared]
    internal class MoveArgumentCodeFixProvider : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0002NamedArgumentPositionMatches.DiagnosticId,
            GU0005ExceptionArgumentsPositions.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) || token.IsMissing)
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
                            "Move arguments to match parameter positions.",
                            (editor, cancellationToken) => ApplyFixGU0002(editor, arguments, cancellationToken),
                            this.GetType(),
                        diagnostic);
                }

                if (diagnostic.Id == GU0005ExceptionArgumentsPositions.DiagnosticId)
                {
                    var argument = (ArgumentSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                    context.RegisterCodeFix(
                            "Move name argument to match parameter positions.",
                            (editor, cancellationToken) => ApplyFixGU0005(editor, argument, cancellationToken),
                            this.GetType(),
                        diagnostic);
                }
            }
        }

        private static void ApplyFixGU0002(DocumentEditor editor, ArgumentListSyntax argumentListSyntax, CancellationToken cancellationToken)
        {
            var arguments = new ArgumentSyntax[argumentListSyntax.Arguments.Count];
            var method = editor.SemanticModel.GetSymbolSafe(argumentListSyntax.Parent, cancellationToken) as IMethodSymbol;
            foreach (var argument in argumentListSyntax.Arguments)
            {
                var index = ParameterIndex(method, argument);
                var oldArg = argumentListSyntax.Arguments[index];
                arguments[index] = argument.WithLeadingTrivia(oldArg.GetLeadingTrivia())
                                           .WithTrailingTrivia(oldArg.GetTrailingTrivia());
            }

            var updated = argumentListSyntax.WithArguments(SyntaxFactory.SeparatedList(arguments, argumentListSyntax.Arguments.GetSeparators()));
            editor.ReplaceNode(argumentListSyntax, updated);
        }

        private static void ApplyFixGU0005(DocumentEditor editor, ArgumentSyntax nameArgument, CancellationToken cancellationToken)
        {
            var argumentListSyntax = nameArgument.FirstAncestorOrSelf<ArgumentListSyntax>();
            var arguments = new ArgumentSyntax[argumentListSyntax.Arguments.Count];
            var method = editor.SemanticModel.GetSymbolSafe(argumentListSyntax.Parent, cancellationToken) as IMethodSymbol;
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
            editor.ReplaceNode(argumentListSyntax, updated);
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
