namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using Gu.Roslyn.CodeFixExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseSpanFix))]
[Shared]
internal class UseSpanFix : DocumentEditorCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Descriptors.GU0026RangeAllocation.Id);

    protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is BracketedArgumentListSyntax { Parent: ElementAccessExpressionSyntax elementAccess })
            {
                context.RegisterCodeFix(
                    "AsSpan()",
                    (editor, _) => editor.ReplaceNode(
                        elementAccess,
                        x => x.WithExpression(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    x.Expression,
                                    SyntaxFactory.IdentifierName("AsSpan"))))),
                    "AsSpan()",
                    diagnostic);
            }
        }
    }
}
