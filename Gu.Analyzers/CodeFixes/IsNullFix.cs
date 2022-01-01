namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using Gu.Roslyn.CodeFixExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IsNullFix))]
[Shared]
internal class IsNullFix : DocumentEditorCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Descriptors.GU0077PreferIsNull.Id);

    protected override DocumentEditorFixAllProvider FixAllProvider() => DocumentEditorFixAllProvider.Solution;

    protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) is BinaryExpressionSyntax binaryExpression)
            {
                context.RegisterCodeFix(
                    binaryExpression.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.IsKeyword).WithTrailingTrivia(SyntaxFactory.Space)).ToString(),
                    e => e.ReplaceNode(
                        binaryExpression,
                        x => x.WithOperatorToken(SyntaxFactory.Token(SyntaxKind.IsKeyword))),
                    nameof(IsNullFix),
                    diagnostic);
            }
        }
    }
}