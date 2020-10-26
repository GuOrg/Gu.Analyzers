﻿namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestMethodParametersFix))]
    [Shared]
    internal class TestMethodParametersFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GU0080TestAttributeCountMismatch.Id,
            Descriptors.GU0083TestCaseAttributeMismatchMethod.Id);

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ParameterListSyntax? parameterList) &&
                    parameterList.Parent is MethodDeclarationSyntax candidate &&
                    TryFindTestAttribute(candidate, semanticModel, context.CancellationToken, out var attribute) &&
                    TryGetParameters(attribute, semanticModel, context.CancellationToken, out var parameters))
                {
                    context.RegisterCodeFix(
                        "Update parameters",
                        (editor, c) =>
                            editor.ReplaceNode(
                                parameterList,
                                x => x.WithParameters(parameters)),
                        nameof(TestMethodParametersFix),
                        diagnostic);
                }
                else if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out attribute) &&
                         attribute.TryFirstAncestor(out MethodDeclarationSyntax? method) &&
                         TryGetParameters(attribute, semanticModel, context.CancellationToken, out parameters))
                {
                    context.RegisterCodeFix(
                        "Update parameters",
                        (editor, c) =>
                            editor.ReplaceNode(
                                method.ParameterList,
                                x => x.WithParameters(parameters)),
                        nameof(TestMethodParametersFix),
                        diagnostic);
                }
            }
        }

        private static bool TryGetParameters(AttributeSyntax testCase, SemanticModel semanticModel, CancellationToken cancellationToken, out SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            if (testCase.ArgumentList is null)
            {
                parameters = default;
                return true;
            }

            if (testCase.ArgumentList is { } argumentList &&
                !argumentList.Arguments.Any(x => x.Expression.IsKind(SyntaxKind.NullLiteralExpression)) &&
                testCase.TryFirstAncestor(out MethodDeclarationSyntax? method) &&
                method.ParameterList is { } current)
            {
                if (argumentList.Arguments.Count == 0)
                {
                    parameters = default;
                    return true;
                }

                parameters = SyntaxFactory.SeparatedList(argumentList.Arguments.Select(syntax => CreateParameter(syntax)));
                return true;

                ParameterSyntax CreateParameter(AttributeArgumentSyntax argument)
                {
                    var i = ((AttributeArgumentListSyntax)argument.Parent).Arguments.IndexOf(argument);
                    if (current!.Parameters.TryElementAt(i, out var parameter))
                    {
                        if (parameter.Modifiers.Any(SyntaxKind.ParamsKeyword))
                        {
                            return parameter.WithType(
                                ((ArrayTypeSyntax)parameter.Type).WithElementType(
                                    SyntaxFactory.ParseTypeName(
                                        semanticModel.GetTypeInfo(argument.Expression, cancellationToken)
                                                     .Type.ToMinimalDisplayString(semanticModel, argument.SpanStart))));
                        }

                        return parameter.WithType(
                            SyntaxFactory.ParseTypeName(
                                             semanticModel.GetTypeInfo(argument.Expression, cancellationToken)
                                                          .Type.ToMinimalDisplayString(semanticModel, argument.SpanStart))
                                         .WithTriviaFrom(parameter.Type));
                    }

                    return SyntaxFactory.Parameter(
                        default,
                        default,
                        SyntaxFactory.ParseTypeName(
                            semanticModel.GetTypeInfo(argument.Expression, cancellationToken)
                                         .Type.ToMinimalDisplayString(semanticModel, argument.SpanStart)),
                        SyntaxFactory.Identifier("arg" + i),
                        null);
                }
            }

            parameters = default;
            return false;
        }

        private static bool TryFindTestAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            attribute = null;
            if (method != null)
            {
                foreach (var attributeList in method.AttributeLists)
                {
                    foreach (var candidate in attributeList.Attributes)
                    {
                        if (semanticModel.TryGetNamedType(candidate, KnownSymbols.NUnitTestCaseAttribute, cancellationToken, out _))
                        {
                            attribute = candidate;
                            return true;
                        }

                        if (semanticModel.TryGetNamedType(candidate, KnownSymbols.NUnitTestAttribute, cancellationToken, out _))
                        {
                            attribute = candidate;
                        }
                    }
                }
            }

            return attribute != null;
        }
    }
}
