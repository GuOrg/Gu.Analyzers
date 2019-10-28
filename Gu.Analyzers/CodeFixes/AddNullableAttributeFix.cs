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
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS8601", "CS8625", "CS8653");

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ExpressionSyntax? expression))
                {
                    if (TryFindLocalOrParameter(out var identifierName) &&
                        TryFindOutParemeter(identifierName.Identifier.ValueText, out var outParameter))
                    {
                        if (diagnostic.Id == "CS8625" ||
                            diagnostic.Id == "CS8601")
                        {
                            context.RegisterCodeFix(
                                "[NotNullWhen(true)]",
                                (editor, _) => editor.ReplaceNode(
                                    outParameter!,
                                    x => outParameter!.WithAttributeList(NotNullWhenTrue)
                                                      .WithType(SyntaxFactory.NullableType(outParameter!.Type)))
                                                     .AddUsing(UsingSystemDiagnostcisCodeAnalysis),
                                "[NotNullWhen(true)]",
                                diagnostic);
                        }
                        else if (diagnostic.Id == "CS8653")
                        {
                            context.RegisterCodeFix(
                                "[MaybeNullWhen(false)]",
                                (editor, _) => editor.ReplaceNode(outParameter!, x => outParameter!.WithAttributeList(MaybeNullWhenFalse))
                                                     .ReplaceNode(expression, x => SyntaxFactory.ParseExpression("default!"))
                                                     .AddUsing(UsingSystemDiagnostcisCodeAnalysis),
                                "[MaybeNullWhen(false)]",
                                diagnostic);
                        }
                    }

                    if (expression.Parent is EqualsValueClauseSyntax equalsValueClause &&
                        equalsValueClause.Parent is ParameterSyntax optionalParameter)
                    {
                        context.RegisterCodeFix(
                            optionalParameter.Type.ToString() + "?",
                            (editor, _) => editor.ReplaceNode(
                                optionalParameter,
                                x => optionalParameter.WithType(SyntaxFactory.NullableType(optionalParameter.Type))),
                            "?",
                            diagnostic);
                    }

                    bool TryFindLocalOrParameter(out IdentifierNameSyntax result)
                    {
                        switch (expression.Parent)
                        {
                            case AssignmentExpressionSyntax assignment when assignment.Left is IdentifierNameSyntax local:
                                result = local;
                                return true;
                            case ArgumentSyntax argument when expression is IdentifierNameSyntax arg:
                                result = arg;
                                return true;
                            default:
                                result = null!;
                                return false;
                        }
                    }

                    bool TryFindOutParemeter(string name, out ParameterSyntax? result)
                    {
                        result = null!;
                        return expression.TryFirstAncestor(out MethodDeclarationSyntax? method) &&
                               method.ReturnType == KnownSymbol.Boolean &&
                               method.TryFindParameter(name, out result) &&
                               result.Modifiers.Any(SyntaxKind.OutKeyword);
                    }
                }
            }
        }
    }
}
