namespace Gu.Analyzers.CodeFixes
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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocsFix))]
    [Shared]
    internal class AddNullableAttributeFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS8625");

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out LiteralExpressionSyntax literal) &&
                    literal.IsKind(SyntaxKind.NullLiteralExpression) &&
                    literal.Parent is AssignmentExpressionSyntax assignment &&
                    assignment.Left is IdentifierNameSyntax left &&
                    assignment.TryFirstAncestor(out MethodDeclarationSyntax? method) &&
                    method.ReturnType == KnownSymbol.Boolean &&
                    method.TryFindParameter(left.Identifier.ValueText, out var parameter) &&
                    parameter.Modifiers.Any(SyntaxKind.OutKeyword))
                {
                    context.RegisterCodeFix(
                        "Add [NotNullWhen(true)].",
                        (editor, _) => editor.ReplaceNode(
                            parameter,
                            x => parameter.WithAttributeListText("[NotNullWhen(true)]")
                                          .WithType(SyntaxFactory.NullableType(parameter.Type))),
                        "Add [NotNullWhen(true)].",
                        diagnostic);
                }
            }
        }
    }
}
