namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseLambdaFix))]
    [Shared]
    internal class UseLambdaFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Descriptors.GU0016PreferLambda.Id);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor<IdentifierNameSyntax>(diagnostic, out var identifierName))
                {
                    context.RegisterCodeFix(
                        "Use lambda.",
                        (editor, cancellationToken) => UseLambda(editor, identifierName, cancellationToken),
                        "Use lambda.",
                        diagnostic);
                }
            }
        }

        private static void UseLambda(DocumentEditor editor, IdentifierNameSyntax identifierName, CancellationToken cancellationToken)
        {
            if (editor.SemanticModel.TryGetSymbol(identifierName, cancellationToken, out IMethodSymbol? method))
            {
                if (method.Parameters.Length == 0)
                {
                    _ = editor.ReplaceNode(
                        identifierName,
                        x => SyntaxFactory.ParseExpression($"() => {x.Identifier.ValueText}()"));
                }
                else if (method.Parameters.TrySingle(out var parameter))
                {
                    var parameterName = SafeParameterName(parameter, identifierName);
                    _ = editor.ReplaceNode(
                        identifierName,
                        x => SyntaxFactory.ParseExpression($"{parameterName} => {x.Identifier.ValueText}({parameterName})"));
                }
                else
                {
                    var parameters = string.Join(", ", method.Parameters.Select(x => SafeParameterName(x, identifierName)));
                    _ = editor.ReplaceNode(
                        identifierName,
                        x => SyntaxFactory.ParseExpression($"({parameters}) => {x.Identifier.ValueText}({parameters})"));
                }
            }
        }

        private static string SafeParameterName(IParameterSymbol parameter, IdentifierNameSyntax context)
        {
            if (context.TryFirstAncestor<MemberDeclarationSyntax>(out var ancestor))
            {
                using (var walker = IdentifierTokenWalker.Borrow(ancestor))
                {
                    var name = parameter.Name;
                    while (walker.TryFind(name, out _))
                    {
                        name += "_";
                    }

                    return name;
                }
            }

            return parameter.Name;
        }
    }
}
