namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenerateUselessDocsFixProvider))]
    [Shared]
    internal class GenerateUselessDocsFixProvider : DocumentEditorCodeFixProvider
    {
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
                    parameterList.Parent is BaseMethodDeclarationSyntax methodDeclaration)
                {
                    context.RegisterCodeFix(
                        "Generate useless xml documentation for parameter.",
                        (editor, _) => AddParameterDocs(editor, parameter, methodDeclaration, _),
                        nameof(GenerateUselessDocsFixProvider),
                        diagnostic);
                }
            }
        }

        private static void AddParameterDocs(DocumentEditor editor, ParameterSyntax parameterSyntax, BaseMethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            if (methodDeclaration.HasLeadingTrivia &&
                methodDeclaration.GetLeadingTrivia() is SyntaxTriviaList leadingTrivia &&
                leadingTrivia.TryFirst(out var first) &&
                first.IsKind(SyntaxKind.WhitespaceTrivia) &&
                leadingTrivia.TrySingle(x => x.HasStructure, out var withStructure) &&
                withStructure.GetStructure() is DocumentationCommentTriviaSyntax comment &&
                editor.SemanticModel.TryGetSymbol(parameterSyntax, cancellationToken, out var parameter) &&
                TryFindPosition(comment, parameter, out var element))
            {
                editor.InsertAfter(element, CreateParameterElements(parameter, first.ToString()));
            }
        }

        private static bool TryFindPosition(DocumentationCommentTriviaSyntax comment, IParameterSymbol parameter, out XmlElementSyntax position)
        {
            position = null;
            if (parameter.ContainingSymbol is IMethodSymbol method)
            {
                position = comment.Content.OfType<XmlElementSyntax>()
                                  .TakeWhile(IsBefore)
                                  .LastOrDefault();
            }

            return position != null;

            bool IsBefore(XmlElementSyntax e)
            {
                if (e.StartTag is XmlElementStartTagSyntax startTag &&
                    startTag.Name is XmlNameSyntax nameSyntax)
                {
                    if (string.Equals("summary", nameSyntax.LocalName.ValueText, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("typeparam", nameSyntax.LocalName.ValueText, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (nameSyntax.LocalName.ValueText == "param" &&
                        startTag.Attributes.TrySingleOfType(out XmlNameAttributeSyntax attribute) &&
                        method.TryFindParameter(attribute.Identifier.Identifier.ValueText, out var other))
                    {
                        return other.Ordinal < parameter.Ordinal;
                    }

                    return false;
                }

                return false;
            }
        }

        private static IEnumerable<XmlNodeSyntax> CreateParameterElements(IParameterSymbol parameter, string leadingWhitespace)
        {
            var code = $"/// <summary> </summary>\r\n{leadingWhitespace}/// <param name=\"{parameter.Name}\">The <see cref=\"{parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}\"/></param>";
            if (SyntaxFactory.ParseLeadingTrivia(code).TrySingle(x => x.HasStructure, out var trivia) &&
                trivia.GetStructure() is DocumentationCommentTriviaSyntax syntax)
            {
                return syntax.Content.Skip(2);
            }

            throw new InvalidOperationException("Bug! should never get here.");
        }
    }
}
