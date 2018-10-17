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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UselessDocsFix))]
    [Shared]
    internal class UselessDocsFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("SA1611", "SA1614");

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out BaseMethodDeclarationSyntax methodDeclaration))
                {
                    if (syntaxRoot.TryFindNode(diagnostic, out ParameterSyntax parameter) &&
                        methodDeclaration.TryGetDocumentationComment(out var docs))
                    {
                        if (StandardDocs.TryGet(parameter, out var text))
                        {
                            context.RegisterCodeFix(
                                "Generate standard xml documentation for parameter.",
                                (editor, _) => editor.ReplaceNode(
                                    docs,
                                    x => x.WithParamText(parameter.Identifier.ValueText, text)),
                                text,
                                diagnostic);
                        }
                        else
                        {
                            context.RegisterCodeFix(
                                "Generate useless xml documentation for parameter.",
                                (editor, _) => editor.ReplaceNode(
                                    docs,
                                    x => x.WithParamText(parameter.Identifier.ValueText, $"The <see cref=\"{parameter.Type.ToString().Replace("<", "{").Replace(">", "}")}\"/>.")),
                                nameof(UselessDocsFix),
                                diagnostic);
                        }
                    }
                    else if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan, true, true) is XmlElementSyntax element &&
                             element.TryGetNameAttribute(out var name) &&
                             methodDeclaration.TryFindParameter(name.Identifier?.Identifier.ValueText, out parameter))
                    {
                        if (StandardDocs.TryGet(parameter, out var text))
                        {
                            context.RegisterCodeFix(
                                "Generate standard xml documentation for parameter.",
                                (editor, _) => editor.ReplaceNode(
                                    element,
                                    x => WithText(x, text)),
                                text,
                                diagnostic);
                        }
                        else
                        {
                            context.RegisterCodeFix(
                                "Generate useless xml documentation for parameter.",
                                (editor, _) => editor.ReplaceNode(
                                    element,
                                    x => WithText(x, $"The <see cref=\"{parameter.Type.ToString().Replace("<", "{").Replace(">", "}")}\"/>.")),
                                nameof(UselessDocsFix),
                                diagnostic);
                        }
                    }
                }
            }
        }

        private static XmlElementSyntax WithText(XmlElementSyntax element, string text)
        {
            return element.WithContent(Parse.XmlElementSyntax($"<param>{text}</param>").Content);
        }
    }
}
