namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocsFix))]
    [Shared]
    internal class DocsFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("SA1611", "SA1614", "SA1618", Descriptors.GU0100WrongDocs.Id);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out BaseMethodDeclarationSyntax methodDeclaration))
                {
                    if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ParameterSyntax parameter) &&
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
                        else if (parameter.Type is PredefinedTypeSyntax)
                        {
                            context.RegisterCodeFix(
                                "Generate useless xml documentation for parameter.",
                                (editor, _) => editor.ReplaceNode(
                                    docs,
                                    x => x.WithParamText(parameter.Identifier.ValueText, string.Empty)),
                                nameof(DocsFix),
                                diagnostic);
                        }
                        else
                        {
                            context.RegisterCodeFix(
                                "Generate useless xml documentation for parameter.",
                                (editor, _) => editor.ReplaceNode(
                                    docs,
                                    x => x.WithParamText(parameter.Identifier.ValueText, $"The <see cref=\"{parameter.Type.ToString().Replace("<", "{").Replace(">", "}")}\"/>.")),
                                nameof(DocsFix),
                                diagnostic);
                        }
                    }
                    else if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out TypeParameterSyntax typeParameter) &&
                             methodDeclaration.TryGetDocumentationComment(out docs))
                    {
                        if (TryGetTypeParameterText(methodDeclaration, typeParameter, semanticModel, context.CancellationToken, out var text))
                        {
                            context.RegisterCodeFix(
                                "Generate useless xml documentation for type parameter.",
                                (editor, _) => editor.ReplaceNode(
                                    docs,
                                    x => x.WithTypeParamText(typeParameter.Identifier.ValueText, text)),
                                nameof(DocsFix),
                                diagnostic);
                        }
                        else
                        {
                            context.RegisterCodeFix(
                                "Generate empty xml documentation for type parameter.",
                                (editor, _) => editor.ReplaceNode(
                                    docs,
                                    x => x.WithTypeParamText(typeParameter.Identifier.ValueText, string.Empty)),
                                nameof(DocsFix),
                                diagnostic);
                        }
                    }
                    else if (TryFindParamDoc(out var element) &&
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
                                nameof(DocsFix),
                                diagnostic);
                        }
                    }
                }

                bool TryFindParamDoc(out XmlElementSyntax result)
                {
                    switch (syntaxRoot.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true))
                    {
                        case XmlElementSyntax element:
                            result = element;
                            return true;
                        case SyntaxNode node when node.TryFirstAncestor(out XmlElementSyntax element):
                            result = element;
                            return true;
                        default:
                            result = null;
                            return false;
                    }
                }
            }
        }

        private static bool TryGetTypeParameterText(BaseMethodDeclarationSyntax methodDeclaration, TypeParameterSyntax typeParameter, SemanticModel semanticModel, CancellationToken cancellationToken, out string text)
        {
            text = null;
            if (methodDeclaration.ParameterList is ParameterListSyntax parameterList)
            {
                foreach (var parameter in parameterList.Parameters)
                {
                    if (IsParameterType(parameter.Type))
                    {
                        if (text != null)
                        {
                            text = null;
                            return false;
                        }

                        text = $"The type of <paramref name=\"{parameter.Identifier.Text}\"/>.";
                    }
                    else if (parameter.Type is ArrayTypeSyntax arrayType &&
                             IsParameterType(arrayType.ElementType))
                    {
                        if (text != null)
                        {
                            text = null;
                            return false;
                        }

                        text = $"The type of the elements in <paramref name=\"{parameter.Identifier.Text}\"/>.";
                    }
                    else if (semanticModel.TryGetType(parameter.Type, cancellationToken, out var type) &&
                             type is INamedTypeSymbol namedType &&
                             namedType.IsGenericType &&
                             type.Interfaces.TrySingle(x => x.MetadataName == "IEnumerable`1", out _))
                    {
                        if (text != null)
                        {
                            text = null;
                            return false;
                        }

                        text = $"The type of the elements in <paramref name=\"{parameter.Identifier.Text}\"/>.";
                    }
                }
            }

            return text != null;

            bool IsParameterType(TypeSyntax typeSyntax)
            {
                return typeSyntax is SimpleNameSyntax simple &&
                       simple.Identifier.Text == typeParameter.Identifier.Text;
            }
        }

        private static XmlElementSyntax WithText(XmlElementSyntax element, string text)
        {
            return element.WithContent(Parse.XmlElementSyntax($"<param>{text}</param>").Content);
        }
    }
}