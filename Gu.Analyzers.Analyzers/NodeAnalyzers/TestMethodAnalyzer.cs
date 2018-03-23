namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class TestMethodAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GU0080TestAttributeCountMismatch.Descriptor,
            GU0081TestCasesAttributeMismatch.Descriptor,
            GU0082IdenticalTestCase.Descriptor,
            GU0083TestCaseAttributeMismatchMethod.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.MethodDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is MethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.ParameterList is ParameterListSyntax parameterList &&
                methodDeclaration.AttributeLists.Count > 0 &&
                context.ContainingSymbol is IMethodSymbol testMethod)
            {
                if (TrySingleTestAttribute(methodDeclaration, context.SemanticModel, context.CancellationToken, out var attribute) &&
                    parameterList.Parameters.Count > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0080TestAttributeCountMismatch.Descriptor, methodDeclaration.Identifier.GetLocation(), parameterList, attribute));
                }

                if (TryFirstTestCaseAttribute(methodDeclaration, context.SemanticModel, context.CancellationToken, out attribute))
                {
                    if (parameterList.Parameters.Count != CountArgs(attribute))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GU0080TestAttributeCountMismatch.Descriptor, methodDeclaration.Identifier.GetLocation(), parameterList, attribute));
                    }

                    foreach (var attributeList in methodDeclaration.AttributeLists)
                    {
                        foreach (var candidate in attributeList.Attributes)
                        {
                            if (Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseAttribute, context.SemanticModel, context.CancellationToken))
                            {
                                if (parameterList.Parameters.Count != CountArgs(candidate))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(GU0081TestCasesAttributeMismatch.Descriptor, candidate.GetLocation(), candidate, parameterList));
                                }

                                if (TryGetFirstMismatch(testMethod, candidate, context, out var argument))
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(GU0083TestCaseAttributeMismatchMethod.Descriptor, argument.GetLocation(), candidate, parameterList));
                                }
                            }

                            if (TryFindIdentical(methodDeclaration, candidate, out _))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(GU0082IdenticalTestCase.Descriptor, candidate.GetLocation(), candidate));
                            }
                        }
                    }
                }
            }
        }

        private static bool TryGetFirstMismatch(IMethodSymbol methodSymbol, AttributeSyntax attributeSyntax, SyntaxNodeAnalysisContext context, out AttributeArgumentSyntax attributeArgument)
        {
            if (methodSymbol.Parameters.Length > 0 &&
                methodSymbol.Parameters != null &&
                attributeSyntax.ArgumentList is AttributeArgumentListSyntax argumentList &&
                argumentList.Arguments.Count > 0 &&
                CountArgs(attributeSyntax) == methodSymbol.Parameters.Length)
            {
                for (var index = 0; index < methodSymbol.Parameters.Length; index++)
                {
                    var argument = argumentList.Arguments[index];
                    var parameter = methodSymbol.Parameters[index];

                    if (argument is null ||
                        argument.NameEquals != null ||
                        parameter is null)
                    {
                        attributeArgument = argument;
                        return true;
                    }

                    if (parameter.Type == KnownSymbol.Object)
                    {
                        continue;
                    }

                    if (argument.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                    {
                        if (parameter.Type.IsValueType &&
                            !parameter.Type.Is(KnownSymbol.NullableOfT))
                        {
                            attributeArgument = argument;
                            return true;
                        }

                        continue;
                    }

                    var argumentType = context.SemanticModel.GetTypeInfoSafe(argument.Expression, context.CancellationToken);
                    if (!argumentType.Type.Is(parameter.Type) &&
                        !context.SemanticModel.ClassifyConversion(argument.Expression, parameter.Type).IsImplicit)
                    {
                        attributeArgument = argument;
                        return true;
                    }
                }
            }

            attributeArgument = null;
            return false;
        }

        private static bool TrySingleTestAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax attribute)
        {
            attribute = null;
            var count = 0;
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var candidate in attributeList.Attributes)
                {
                    if (Attribute.IsType(candidate, KnownSymbol.NUnitTestAttribute, semanticModel, cancellationToken))
                    {
                        attribute = candidate;
                        count++;
                    }
                    else if (Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseAttribute, semanticModel, cancellationToken) ||
                           Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseSourceAttribute, semanticModel, cancellationToken))
                    {
                        count++;
                    }
                }
            }

            return count == 1 && attribute != null;
        }

        private static bool TryFirstTestCaseAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax attribute)
        {
            attribute = null;
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var candidate in attributeList.Attributes)
                {
                    if (Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseAttribute, semanticModel, cancellationToken))
                    {
                        attribute = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryFindIdentical(MethodDeclarationSyntax method, AttributeSyntax attribute, out AttributeSyntax identical)
        {
            if (Attribute.TryGetTypeName(attribute, out var name))
            {
                foreach (var attributeList in method.AttributeLists)
                {
                    foreach (var candidate in attributeList.Attributes)
                    {
                        if (Attribute.TryGetTypeName(candidate, out var candidateName) &&
                            name == candidateName &&
                            !ReferenceEquals(candidate, attribute) &&
                            IsIdentical(attribute, candidate))
                        {
                            identical = candidate;
                            return true;
                        }
                    }
                }
            }

            identical = null;
            return false;

            bool IsIdentical(AttributeSyntax x, AttributeSyntax y)
            {
                for (var i = 0; i < Math.Min(x.ArgumentList.Arguments.Count, y.ArgumentList.Arguments.Count); i++)
                {
                    var xa = x.ArgumentList.Arguments[i];
                    var ya = y.ArgumentList.Arguments[i];
                    if (xa.NameEquals != null ||
                        ya.NameEquals != null)
                    {
                        return xa.NameEquals != null && ya.NameEquals != null;
                    }

                    if (xa.Expression is LiteralExpressionSyntax xl &&
                        ya.Expression is LiteralExpressionSyntax yl)
                    {
                        if (xl.Token.Text != yl.Token.Text)
                        {
                            return false;
                        }
                    }
                    else if (TryGetArrayExpressions(xa.Expression, out var xExpressions) &&
                             TryGetArrayExpressions(ya.Expression, out var yExpressions))
                    {
                        {
                            if (xExpressions.Count != yExpressions.Count)
                            {
                                return false;
                            }

                            for (var j = 0; j < xExpressions.Count; j++)
                            {
                                if (xExpressions[j] is LiteralExpressionSyntax xLiteral &&
                                    yExpressions[j] is LiteralExpressionSyntax yLiteral &&
                                    xLiteral.Token.Text != yLiteral.Token.Text)
                                {
                                    return false;
                                }
                            }

                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private static int CountArgs(AttributeSyntax attribute)
        {
            var count = 0;
            if (attribute?.ArgumentList is AttributeArgumentListSyntax argumentList)
            {
                foreach (var argument in argumentList.Arguments)
                {
                    if (argument.NameEquals == null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool TryGetArrayExpressions(ExpressionSyntax expression, out SeparatedSyntaxList<ExpressionSyntax> expressions)
        {
            expressions = default(SeparatedSyntaxList<ExpressionSyntax>);
            if (expression is ImplicitArrayCreationExpressionSyntax implicitArrayCreation &&
                implicitArrayCreation.Initializer != null)
            {
                expressions = implicitArrayCreation.Initializer.Expressions;
                return true;
            }

            if (expression is ArrayCreationExpressionSyntax arrayCreation &&
                arrayCreation.Initializer != null)
            {
                expressions = arrayCreation.Initializer.Expressions;
                return true;
            }

            return false;
        }
    }
}
