namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Analyzers.DependencyProperties;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NameArgumentsCodeFixProvider))]
    [Shared]
    internal class NameArgumentsCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0001NameArguments.DiagnosticId);

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

                var arguments = (ArgumentListSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (HasAnyNamedArgument(arguments))
                {
                    continue;
                }

                var method = semanticModel.SemanticModelFor(arguments.Parent)
                          .GetSymbolInfo(arguments.Parent, context.CancellationToken)
                          .Symbol as IMethodSymbol;
                if (method == null)
                {
                    continue;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Name arguments",
                        _ => ApplyFixAsync(context, syntaxRoot, method, arguments),
                        nameof(NameArgumentsCodeFixProvider)),
                    diagnostic);
            }
        }

        private static async Task<Document> ApplyFixAsync(CodeFixContext context, SyntaxNode syntaxRoot, IMethodSymbol method, ArgumentListSyntax arguments)
        {
            var withNames = arguments;
            for (int i = 0; i < arguments.Arguments.Count; i++)
            {
                var argument = withNames.Arguments[i];
                var withNameColon = argument.WithNameColon(SyntaxFactory.NameColon(method.Parameters[i].Name));
                withNames = withNames.ReplaceNode(argument, withNameColon);
            }

            return context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(arguments, withNames));
        }

        private static bool HasAnyNamedArgument(ArgumentListSyntax argumentList)
        {
            foreach (var argument in argumentList.Arguments)
            {
                if (argument.NameColon != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
