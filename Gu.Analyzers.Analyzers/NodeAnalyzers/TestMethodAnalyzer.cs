namespace Gu.Analyzers
{
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
            GU0081TestCasesAttributeMismatch.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleAssignment, SyntaxKind.MethodDeclaration);
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is MethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.ParameterList is ParameterListSyntax parameterList &&
                methodDeclaration.AttributeLists.Count > 0)
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
                            if (Attribute.IsType(candidate, KnownSymbol.NUnitTestCaseAttribute, context.SemanticModel, context.CancellationToken) &&
                                parameterList.Parameters.Count != CountArgs(candidate))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(GU0081TestCasesAttributeMismatch.Descriptor, candidate.GetLocation(), candidate, parameterList));
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
    }
}
