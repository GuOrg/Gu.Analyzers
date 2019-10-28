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

        private static readonly AttributeListSyntax MaybeNullWhenFalse = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("MaybeNullWhen"),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))))));

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS8625", "CS8653");

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ExpressionSyntax? expression) &&
                    expression.Parent is AssignmentExpressionSyntax assignment &&
                    assignment.Left is IdentifierNameSyntax left &&
                    assignment.TryFirstAncestor(out MethodDeclarationSyntax? method) &&
                    method.ReturnType == KnownSymbol.Boolean &&
                    method.TryFindParameter(left.Identifier.ValueText, out var parameter) &&
                    parameter.Modifiers.Any(SyntaxKind.OutKeyword))
                {
                    if (diagnostic.Id == "CS8625")
                    {
                        context.RegisterCodeFix(
                            "[NotNullWhen(true)]",
                            (editor, _) => editor.ReplaceNode(
                                parameter,
                                x => parameter.WithAttributeList(NotNullWhenTrue)
                                              .WithType(SyntaxFactory.NullableType(parameter.Type)))
                                                 .AddUsing(UsingSystemDiagnostcisCodeAnalysis),
                            "[NotNullWhen(true)]",
                            diagnostic);
                    }
                    else if (diagnostic.Id == "CS8653")
                    {
                        context.RegisterCodeFix(
                            "[MaybeNullWhen(false)]",
                            (editor, _) => editor.ReplaceNode(parameter, x => parameter.WithAttributeList(MaybeNullWhenFalse))
                                                 .ReplaceNode(expression, x => SyntaxFactory.ParseExpression("default!"))
                                                 .AddUsing(UsingSystemDiagnostcisCodeAnalysis),
                            "[MaybeNullWhen(false)]",
                            diagnostic);
                    }
                }
            }
        }
    }
}
