namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0080TestAttributeCountMismatch,
            Descriptors.GU0081TestCasesAttributeMismatch,
            Descriptors.GU0082IdenticalTestCase,
            Descriptors.GU0083TestCaseAttributeMismatchMethod);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.MethodDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is MethodDeclarationSyntax { ParameterList: { } parameterList } methodDeclaration &&
                methodDeclaration.AttributeLists.Count > 0 &&
                context.ContainingSymbol is IMethodSymbol testMethod)
            {
                if (TrySingleTestAttribute(methodDeclaration, context.SemanticModel, context.CancellationToken, out var attribute) &&
                    testMethod.Parameters.Length > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0080TestAttributeCountMismatch, parameterList.GetLocation(), parameterList, attribute));
                }

                if (TrySingleTestCaseAttribute(methodDeclaration, context.SemanticModel, context.CancellationToken, out attribute) &&
                    !CountMatches(testMethod, attribute))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0080TestAttributeCountMismatch, parameterList.GetLocation(), parameterList, attribute));
                }

                foreach (var attributeList in methodDeclaration.AttributeLists)
                {
                    foreach (var candidate in attributeList.Attributes)
                    {
                        if (context.SemanticModel.TryGetNamedType(candidate, KnownSymbols.NUnitTestCaseAttribute, context.CancellationToken, out _))
                        {
                            if (!CountMatches(testMethod, candidate))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0081TestCasesAttributeMismatch, candidate.GetLocation(), parameterList, attribute));
                            }

                            if (TryFindIdentical(methodDeclaration, candidate, context, out _))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0082IdenticalTestCase, candidate.GetLocation(), candidate));
                            }

                            if (TryGetFirstMismatch(testMethod, candidate, context, out var argument))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0083TestCaseAttributeMismatchMethod, argument.GetLocation(), candidate, parameterList));
                            }
                        }
                    }
                }
            }
        }

        private static bool TrySingleTestAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            attribute = null;
            var count = 0;
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var candidate in attributeList.Attributes)
                {
                    if (semanticModel.TryGetNamedType(candidate, KnownSymbols.NUnitTestAttribute, cancellationToken, out _))
                    {
                        attribute = candidate;
                        count++;
                    }
                    else if (semanticModel.TryGetNamedType(candidate, KnownSymbols.NUnitTestCaseAttribute, cancellationToken, out _) ||
                             semanticModel.TryGetNamedType(candidate, KnownSymbols.NUnitTestCaseSourceAttribute, cancellationToken, out _))
                    {
                        count++;
                    }
                }
            }

            return count == 1 && attribute != null;
        }

        private static bool TrySingleTestCaseAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            attribute = null;
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var candidate in attributeList.Attributes)
                {
                    if (semanticModel.TryGetNamedType(candidate, KnownSymbols.NUnitTestCaseAttribute, cancellationToken, out _))
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

        private static bool TryGetFirstMismatch(IMethodSymbol methodSymbol, AttributeSyntax attributeSyntax, SyntaxNodeAnalysisContext context, [NotNullWhen(true)] out AttributeArgumentSyntax? attributeArgument)
        {
            attributeArgument = null;
            if (methodSymbol.Parameters.Length > 0 &&
                attributeSyntax is { ArgumentList: { Arguments: { } arguments } } &&
                arguments.Count > 0)
            {
                for (var i = 0; i < Math.Min(CountArgs(attributeSyntax), methodSymbol.Parameters.Length); i++)
                {
                    var argument = arguments[i];
                    var parameter = methodSymbol.Parameters[i];

                    if (argument is null ||
                        argument.NameEquals != null ||
                        parameter is null)
                    {
                        attributeArgument = argument;
                        return true;
                    }

                    if (parameter is { IsParams: true } &&
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
                if (parameterType == KnownSymbols.Object)
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
                    return !parameterType.IsValueType || parameterType.Name == "Nullable";
                }

                if (!argument.Expression.IsAssignableTo(parameterType, context.SemanticModel))
                {
                    return false;
                }

                return true;
            }
        }

        private static bool TryFindIdentical(MethodDeclarationSyntax method, AttributeSyntax attribute, SyntaxNodeAnalysisContext context, [NotNullWhen(true)] out AttributeSyntax? identical)
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
                if (x.ArgumentList is null &&
                    y.ArgumentList is null)
                {
                    return true;
                }

                if (x.ArgumentList is null ||
                    y.ArgumentList is null)
                {
                    return false;
                }

                if (x.ArgumentList.Arguments.LastIndexOf(a => a.NameEquals is null) !=
                    y.ArgumentList.Arguments.LastIndexOf(a => a.NameEquals is null))
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
                        if (xl.Token.Text == yl.Token.Text)
                        {
                            continue;
                        }

                        return false;
                    }

                    if (xa.Expression is IdentifierNameSyntax xn &&
                        ya.Expression is IdentifierNameSyntax yn)
                    {
                        if (xn.Identifier.ValueText == yn.Identifier.ValueText &&
                            context.SemanticModel.TryGetSymbol(xn, context.CancellationToken, out var xs) &&
                            context.SemanticModel.TryGetSymbol(yn, context.CancellationToken, out var ys) &&
                            SymbolComparer.Equal(xs, ys))
                        {
                            continue;
                        }

                        return false;
                    }

                    if (xa.Expression is MemberAccessExpressionSyntax xma &&
                        ya.Expression is MemberAccessExpressionSyntax yma)
                    {
                        if (xma.Name.Identifier.ValueText == yma.Name.Identifier.ValueText &&
                            context.SemanticModel.TryGetSymbol(xma, context.CancellationToken, out var xs) &&
                            context.SemanticModel.TryGetSymbol(yma, context.CancellationToken, out var ys) &&
                            SymbolComparer.Equal(xs, ys))
                        {
                            continue;
                        }

                        return false;
                    }

                    if (TryGetArrayExpressions(xa.Expression, out var xExpressions) &&
                        TryGetArrayExpressions(ya.Expression, out var yExpressions))
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

                        continue;
                    }

                    return false;
                }

                return true;
            }
        }

        private static int CountArgs(AttributeSyntax attribute)
        {
            var count = 0;
            if (attribute is { ArgumentList: { Arguments: { } arguments } })
            {
                foreach (var argument in arguments)
                {
                    if (argument.NameEquals is null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool TryGetArrayExpressions(ExpressionSyntax expression, out SeparatedSyntaxList<ExpressionSyntax> expressions)
        {
            switch (expression)
            {
                case ImplicitArrayCreationExpressionSyntax { Initializer: { } initializer }:
                    expressions = initializer.Expressions;
                    return true;
                case ArrayCreationExpressionSyntax { Initializer: { } initializer }:
                    expressions = initializer.Expressions;
                    return true;
                default:
                    expressions = default;
                    return false;
            }
        }
    }
}
