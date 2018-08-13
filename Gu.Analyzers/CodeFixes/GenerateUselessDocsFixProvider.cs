namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenerateUselessDocsFixProvider))]
    [Shared]
    internal class GenerateUselessDocsFixProvider : DocumentEditorCodeFixProvider
    {
        private const string GenerateStandardXmlDocumentationForParameter = "Generate standard xml documentation for parameter.";

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("SA1611");

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ParameterSyntax parameter) &&
                    parameter.Parent is ParameterListSyntax parameterList &&
                    parameterList.Parent is BaseMethodDeclarationSyntax methodDeclaration &&
                    methodDeclaration.TryGetDocumentationComment(out var docs))
                {
                    if (parameter.Type == KnownSymbol.CancellationToken)
                    {
                        context.RegisterCodeFix(
                            GenerateStandardXmlDocumentationForParameter,
                            (editor, _) => editor.ReplaceNode(docs, docs.WithParamText(parameter.Identifier.ValueText, $"The <see cref=\"{parameter.Type.ToString()}\"/> that the task will observe.")),
                            GenerateStandardXmlDocumentationForParameter,
                            diagnostic);
                    }
                    else
                    {
                        context.RegisterCodeFix(
                            "Generate useless xml documentation for parameter.",
                            (editor, _) => editor.ReplaceNode(docs, docs.WithParamText(parameter.Identifier.ValueText, $"The <see cref=\"{parameter.Type.ToString()}\"/>.")),
                            nameof(GenerateUselessDocsFixProvider),
                            diagnostic);
                    }
                }
            }
        }
    }
}
