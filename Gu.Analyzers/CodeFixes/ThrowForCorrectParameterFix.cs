namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ThrowForCorrectParameterFix))]
    [Shared]
    internal class ThrowForCorrectParameterFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GU0013TrowForCorrectParameter.Id);

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) is ArgumentSyntax argument &&
                    diagnostic.Properties.TryGetValue(nameof(IdentifierNameSyntax), out var name))
                {
                    context.RegisterCodeFix(
                        $"Use nameof({name}).",
                        (editor, _) => editor.ReplaceNode(
                            argument.Expression,
                            x => CreateNode(x).WithTriviaFrom(x)),
                        this.GetType(),
                        diagnostic);

                    ExpressionSyntax CreateNode(ExpressionSyntax old)
                    {
                        return old switch
                        {
                            LiteralExpressionSyntax literal
                                when literal.IsKind(SyntaxKind.StringLiteralExpression)
                                => SyntaxFactory.ParseExpression($"nameof({name})"),
                            IdentifierNameSyntax { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } } identifierName
                                when invocation.IsNameOf()
                                => identifierName.WithIdentifier(SyntaxFactory.Identifier(name!)),
                            _ => throw new InvalidOperationException("Failed updating parameter name."),
                        };
                    }
                }
            }
        }
    }
}
