﻿namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveArgumentFix))]
    [Shared]
    internal class MoveArgumentFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GU0002NamedArgumentPositionMatches.Id,
            Descriptors.GU0005ExceptionArgumentsPositions.Id);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == Descriptors.GU0002NamedArgumentPositionMatches.Id &&
                    syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ArgumentListSyntax? argumentList) &&
                    HasWhitespaceTriviaOnly(argumentList))
                {
                    context.RegisterCodeFix(
                              "Move arguments to match parameter positions.",
                              (editor, _) => editor.ReplaceNode(
                                  argumentList,
                                  x => Replacement(x, editor)),
                              this.GetType(),
                              diagnostic);

                    static ArgumentListSyntax Replacement(ArgumentListSyntax old, DocumentEditor editor)
                    {
                        if (editor.SemanticModel.GetSpeculativeSymbolInfo(old.SpanStart, old.Parent, SpeculativeBindingOption.BindAsExpression).Symbol is IMethodSymbol method)
                        {
                            return old.WithArguments(
                                SyntaxFactory.SeparatedList(
                                    old.Arguments.OrderBy(x => ParameterIndex(method, x)),
                                    old.Arguments.GetSeparators()));
                        }

                        return old;
                    }
                }

                if (diagnostic.Id == Descriptors.GU0005ExceptionArgumentsPositions.Id &&
                    syntaxRoot.TryFindNode(diagnostic, out ArgumentSyntax? argument))
                {
                    context.RegisterCodeFix(
                        "Move named argument to match parameter positions.",
                        (editor, cancellationToken) => ApplyFixGU0005(editor, argument, cancellationToken),
                        this.GetType(),
                        diagnostic);
                }
            }
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
            for (var i = 0; i < method.Parameters.Length; i++)
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
