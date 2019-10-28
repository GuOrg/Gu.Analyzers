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
        private static readonly UsingDirectiveSyntax UsingSystemDiagnostcisCodeAnalysis = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Diagnostics.CodeAnalysis"));
        private static readonly AttributeListSyntax NotNullWhenTrue = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("NotNullWhen"),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))))));

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
                            x => parameter.WithAttributeList(NotNullWhenTrue)
                                          .WithType(SyntaxFactory.NullableType(parameter.Type)))
                                             .AddUsing(UsingSystemDiagnostcisCodeAnalysis),
                        "Add [NotNullWhen(true)].",
                        diagnostic);
                }
            }
        }
    }
}
