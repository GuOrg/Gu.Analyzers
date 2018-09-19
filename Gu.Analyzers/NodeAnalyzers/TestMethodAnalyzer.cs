namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
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
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.MethodDeclaration);
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
                    testMethod.Parameters.Length > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0080TestAttributeCountMismatch.Descriptor, parameterList.GetLocation(), parameterList, attribute));
                }

                if (TrySingleTestCaseAttribute(methodDeclaration, context.SemanticModel, context.CancellationToken, out attribute) &&
                    !CountMatches(testMethod, attribute))
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0080TestAttributeCountMismatch.Descriptor, parameterList.GetLocation(), parameterList, attribute));
                }

                foreach (var attributeList in methodDeclaration.AttributeLists)
                {
                    foreach (var candidate in attributeList.Attributes)
                    {
                        if (Roslyn.AnalyzerExtensions.Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseAttribute, context.SemanticModel, context.CancellationToken))
                        {
                            if (!CountMatches(testMethod, candidate))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(GU0081TestCasesAttributeMismatch.Descriptor, candidate.GetLocation(), parameterList, attribute));
                            }

                            if (TryFindIdentical(methodDeclaration, candidate, context, out _))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(GU0082IdenticalTestCase.Descriptor, candidate.GetLocation(), candidate));
                            }

                            if (TryGetFirstMismatch(testMethod, candidate, context, out var argument))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(GU0083TestCaseAttributeMismatchMethod.Descriptor, argument.GetLocation(), candidate, parameterList));
                            }
                        }
                    }
                }
            }
        }

        private static bool TrySingleTestAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax attribute)
        {
            attribute = null;
            var count = 0;
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var candidate in attributeList.Attributes)
                {
                    if (Roslyn.AnalyzerExtensions.Attribute.IsType(candidate, KnownSymbol.NUnitTestAttribute, semanticModel, cancellationToken))
                    {
                        attribute = candidate;
                        count++;
                    }
                    else if (Roslyn.AnalyzerExtensions.Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseAttribute, semanticModel, cancellationToken) ||
                             Roslyn.AnalyzerExtensions.Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseSourceAttribute, semanticModel, cancellationToken))
                    {
                        count++;
                    }
                }
            }

            return count == 1 && attribute != null;
        }

        private static bool TrySingleTestCaseAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax attribute)
        {
            attribute = null;
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var candidate in attributeList.Attributes)
                {
                    if (Roslyn.AnalyzerExtensions.Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseAttribute, semanticModel, cancellationToken))
                    {
                        if (attribute != null)
                        {
                            return false;
                        }

                        attribute = candidate;
                    }
                }
            }

            return attribute != null;
        }

        private static bool CountMatches(IMethodSymbol method, AttributeSyntax attribute)
        {
            if (method.Parameters.TryLast(out var lastParameter) &&
                lastParameter.IsParams)
            {
                return CountArgs(attribute) >= method.Parameters.Length - 1;
            }

            return CountArgs(attribute) == method.Parameters.Length;
        }

        private static bool TryGetFirstMismatch(IMethodSymbol methodSymbol, AttributeSyntax attributeSyntax, SyntaxNodeAnalysisContext context, out AttributeArgumentSyntax attributeArgument)
        {
            attributeArgument = null;
            if (methodSymbol.Parameters.Length > 0 &&
                methodSymbol.Parameters != null &&
                attributeSyntax.ArgumentList is AttributeArgumentListSyntax argumentList &&
                argumentList.Arguments.Count > 0)
            {
                for (var i = 0; i < Math.Min(CountArgs(attributeSyntax), methodSymbol.Parameters.Length); i++)
                {
                    var argument = argumentList.Arguments[i];
                    var parameter = methodSymbol.Parameters[i];

                    if (argument is null ||
                        argument.NameEquals != null ||
                        parameter is null)
                    {
                        attributeArgument = argument;
                        return true;
                    }

                    if (parameter.IsParams &&
                        parameter.Type is IArrayTypeSymbol arrayType)
                    {
                        for (var j = i; j < CountArgs(attributeSyntax); j++)
                        {
                            if (!IsTypeMatch(arrayType.ElementType, argument))
                            {
                                attributeArgument = argument;
                                return true;
                            }
                        }

                        return false;
                    }

                    if (!IsTypeMatch(parameter.Type, argument))
                    {
                        attributeArgument = argument;
                        return true;
                    }
                }
            }

            return false;

            bool IsTypeMatch(ITypeSymbol parameterType, AttributeArgumentSyntax argument)
            {
                if (parameterType == KnownSymbol.Object)
                {
                    return true;
                }

                if (parameterType is ITypeParameterSymbol typeParameter)
                {
                    foreach (var constraintType in typeParameter.ConstraintTypes)
                    {
                        if (constraintType is INamedTypeSymbol namedType &&
                            namedType.IsGenericType &&
                            namedType.TypeArguments.Any(x => x is ITypeParameterSymbol))
                        {
                            // Lazy here.
                            continue;
                        }

                        if (!IsTypeMatch(constraintType, argument))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                if (argument.Expression.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    if (parameterType.IsValueType &&
                        parameterType.Name != "Nullable")
                    {
                        return false;
                    }

                    return true;
                }

                if (!argument.Expression.IsAssignableTo(parameterType, context.SemanticModel))
                {
                    return false;
                }

                return true;
            }
        }

        private static bool TryFindIdentical(MethodDeclarationSyntax method, AttributeSyntax attribute, SyntaxNodeAnalysisContext context, out AttributeSyntax identical)
        {
            if (Roslyn.AnalyzerExtensions.Attribute.TryGetTypeName(attribute, out var name))
            {
                foreach (var attributeList in method.AttributeLists)
                {
                    foreach (var candidate in attributeList.Attributes)
                    {
                        if (Roslyn.AnalyzerExtensions.Attribute.TryGetTypeName(candidate, out var candidateName) &&
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
                if (x.ArgumentList == null &&
                    y.ArgumentList == null)
                {
                    return true;
                }

                if (x.ArgumentList == null ||
                    y.ArgumentList == null)
                {
                    return false;
                }

                if (x.ArgumentList.Arguments.LastIndexOf(a => a.NameEquals == null) !=
                    y.ArgumentList.Arguments.LastIndexOf(a => a.NameEquals == null))
                {
                    return false;
                }

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
                    else if (xa.Expression is IdentifierNameSyntax xn &&
                             ya.Expression is IdentifierNameSyntax yn)
                    {
                        return xn.Identifier.ValueText == yn.Identifier.ValueText &&
                               context.SemanticModel.TryGetSymbol(xn, context.CancellationToken, out ISymbol xs) &&
                               context.SemanticModel.TryGetSymbol(yn, context.CancellationToken, out ISymbol ys) &&
                               xs.Equals(ys);
                    }
                    else if (xa.Expression is MemberAccessExpressionSyntax xma &&
                             ya.Expression is MemberAccessExpressionSyntax yma)
                    {
                        return xma.Name.Identifier.ValueText == yma.Name.Identifier.ValueText &&
                               context.SemanticModel.TryGetSymbol(xma, context.CancellationToken, out ISymbol xs) &&
                               context.SemanticModel.TryGetSymbol(yma, context.CancellationToken, out ISymbol ys) &&
                               xs.Equals(ys);
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
