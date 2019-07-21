namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NameArgumentsFix))]
    [Shared]
    internal class NameArgumentsFix : DocumentEditorCodeFixProvider
    {
        private static readonly SyntaxTriviaList SpaceTrivia = SyntaxTriviaList.Empty.Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "));

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0001NameArguments.Descriptor.Id,
            GU0009UseNamedParametersForBooleans.Descriptor.Id);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
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

                if (diagnostic.Id == GU0001NameArguments.Descriptor.Id &&
                    syntaxRoot.TryFindNode<ArgumentListSyntax>(diagnostic, out var arguments) &&
                    !HasAnyNamedArgument(arguments) &&
                    semanticModel.TryGetSymbol(arguments.Parent, context.CancellationToken, out IMethodSymbol method))
                {
                    context.RegisterCodeFix(
                        "Name arguments",
                        (editor, _) => ApplyFix(editor, method, arguments, 0),
                        this.GetType(),
                        diagnostic);
                }
                else if (diagnostic.Id == GU0009UseNamedParametersForBooleans.Descriptor.Id &&
                         syntaxRoot.TryFindNode<ArgumentSyntax>(diagnostic, out var boolArgument) &&
                         boolArgument.Parent is ArgumentListSyntax argumentList &&
                         !HasAnyNamedArgument(argumentList) &&
                         semanticModel.TryGetSymbol(argumentList.Parent, context.CancellationToken, out method))
                {
                    context.RegisterCodeFix(
                        "Name arguments",
                        (editor, _) => ApplyFix(editor, method, argumentList, argumentList.Arguments.IndexOf(boolArgument)),
                        this.GetType(),
                        diagnostic);
                }
            }
        }

        private static void ApplyFix(DocumentEditor editor, IMethodSymbol method, ArgumentListSyntax arguments, int startIndex)
        {
            var withNames = arguments;
            for (var i = startIndex; i < arguments.Arguments.Count; i++)
            {
                var argument = withNames.Arguments[i];

                editor.ReplaceNode(
                    argument,
                    argument.WithLeadingTrivia(SpaceTrivia)
                            .WithNameColon(
                                SyntaxFactory.NameColon(method.Parameters[i].Name)
                                             .WithLeadingTriviaFrom(argument))
                            .WithTrailingTriviaFrom(argument));
            }

            editor.ReplaceNode(arguments, withNames);
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
