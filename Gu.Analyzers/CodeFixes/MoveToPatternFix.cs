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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveToPatternFix))]
    [Shared]
    internal class MoveToPatternFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GU0074PreferPattern.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out MemberAccessExpressionSyntax? memberAccess) &&
                    memberAccess.Name is IdentifierNameSyntax leftName)
                {
                    if (diagnostic.AdditionalLocations.Count == 0)
                    {
                        context.RegisterCodeFix(
                            $"{memberAccess.Expression} is {{ {leftName}: true }}",
                            (e, c) => e.ReplaceNode(
                                memberAccess,
                                SyntaxFactory.IsPatternExpression(
                                    memberAccess.Expression,
                                    SyntaxFactory.RecursivePattern(
                                        null,
                                        null,
                                        SyntaxFactory.PropertyPatternClause(SyntaxFactory.SingletonSeparatedList(IsTrue(leftName))),
                                        null))),
                            "Use pattern",
                            diagnostic);
                    }
                    else if (diagnostic.AdditionalLocations.TrySingle(out var additionalLocation) &&
                             syntaxRoot.TryFindNode(additionalLocation, out IsPatternExpressionSyntax? pattern) &&
                             memberAccess.TryFindSharedAncestorRecursive(pattern, out BinaryExpressionSyntax? binary))
                    {
                        context.RegisterCodeFix(
                            $"Merge with pattern.",
                            (e, c) => e.ReplaceNode(
                                binary,
                                x => Merge(x)),
                            "Use pattern",
                            diagnostic);

                        ExpressionSyntax Merge(BinaryExpressionSyntax old)
                        {
                            if (old.Left is IsPatternExpressionSyntax { Pattern: RecursivePatternSyntax recursive } isPattern &&
                                old.Right is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax right })
                            {
                                return isPattern.WithPattern(recursive.AddPropertyPatternClauseSubpatterns(IsTrue(right)));
                            }

                            return old;
                        }
                    }
                }

                static SubpatternSyntax IsTrue(IdentifierNameSyntax identifierName)
                {
                    return SyntaxFactory.Subpattern(
                        nameColon: SyntaxFactory.NameColon(identifierName),
                        pattern: SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
                }
            }
        }
    }
}
