namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestMethodParametersFix))]
    [Shared]
    internal class TestMethodParametersFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(GU0080TestAttributeCountMismatch.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ParameterListSyntax parameterList) &&
                    parameterList.Parent is MethodDeclarationSyntax method)
                {
                    if (TryFirstTestCaseAttribute(method, semanticModel, context.CancellationToken, out var attribute))
                    {
                        if (parameterList.Parameters.Count == 0)
                        {
                            context.RegisterCodeFix(
                                "Add parameters",
                                (editor, c) =>
                                    editor.ReplaceNode(
                                        parameterList,
                                        x => x.WithParameters(x.Parameters.AddRange(CreateParameters(attribute.ArgumentList, semanticModel, c)))),
                                nameof(TestMethodParametersFix),
                                diagnostic);
                        }
                    }
                    else
                    {
                        context.RegisterCodeFix(
                            "Remove parameters",
                            (editor, _) =>
                                editor.ReplaceNode(
                                    parameterList,
                                    x => x.WithParameters(default(SeparatedSyntaxList<ParameterSyntax>))),
                            nameof(TestMethodParametersFix),
                            diagnostic);
                    }
                }
            }
        }

        private static IEnumerable<ParameterSyntax> CreateParameters(AttributeArgumentListSyntax attributeArgumentList, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            for (var i = 0; i < attributeArgumentList.Arguments.Count; i++)
            {
                var argument = attributeArgumentList.Arguments[i];
                yield return SyntaxFactory.Parameter(
                    default(SyntaxList<AttributeListSyntax>),
                    default(SyntaxTokenList),
                    SyntaxFactory.ParseTypeName(
                        semanticModel.GetTypeInfo(argument.Expression)
                                     .Type.ToMinimalDisplayString(
                                         semanticModel,
                                         attributeArgumentList.SpanStart)),
                    SyntaxFactory.Identifier("arg" + i),
                    null);
            }
        }

        private static bool TryFirstTestCaseAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax attribute)
        {
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var candidate in attributeList.Attributes)
                {
                    if (Roslyn.AnalyzerExtensions.Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseAttribute, semanticModel, cancellationToken))
                    {
                        attribute = candidate;
                        return true;
                    }
                }
            }

            attribute = null;
            return false;
        }
    }
}
