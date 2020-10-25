namespace Gu.Analyzers
{
    using System.Collections.Immutable;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0011DoNotIgnoreReturnValue : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0011DoNotIgnoreReturnValue);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => HandleCreation(c), SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(c => HandleInvocation(c), SyntaxKind.InvocationExpression);
        }

        private static void HandleCreation(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is ObjectCreationExpressionSyntax objectCreation &&
                IsIgnored(objectCreation))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0011DoNotIgnoreReturnValue, context.Node.GetLocation()));
            }
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is InvocationExpressionSyntax invocation &&
                IsIgnored(invocation) &&
                !CanIgnore(invocation, context))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0011DoNotIgnoreReturnValue, context.Node.GetLocation()));
            }
        }

        private static bool IsIgnored(SyntaxNode node)
        {
            return node.Parent is ExpressionStatementSyntax { Parent: BlockSyntax _ };
        }

        private static bool CanIgnore(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
        {
            if (context.SemanticModel.TryGetSymbol(invocation, context.CancellationToken, out var method))
            {
                if (method.ReturnsVoid ||
                    method.ContainingType.IsAssignableTo(KnownSymbol.GuInjectKernel, context.Compilation) ||
                    method.ContainingType.IsAssignableTo(KnownSymbol.MoqMockOfT, context.Compilation) ||
                    method.ContainingType.IsAssignableTo(KnownSymbol.MoqIFluentInterface, context.Compilation) ||
                    method.ContainingType.IsAssignableTo(KnownSymbol.NinjectIFluentSyntax, context.Compilation))
                {
                    return true;
                }

                if (Equals(method.ContainingType, method.ReturnType) &&
                    method.ContainingType == KnownSymbol.StringBuilder)
                {
                    return true;
                }

                if ((method.Name == "Add" ||
                     method.Name == "Remove" ||
                     method.Name == "RemoveAll" ||
                     method.Name == "TryAdd" ||
                     method.Name == "TryRemove") &&
                    method.ReturnType.IsEither(KnownSymbol.Boolean, KnownSymbol.Int32, KnownSymbol.Int64))
                {
                    return true;
                }

                if (method.IsExtensionMethod)
                {
                    method = method.ReducedFrom;
                }

                if (method.TrySingleDeclaration(context.CancellationToken, out MethodDeclarationSyntax? declaration))
                {
                    using var walker = ReturnValueWalker.Borrow(declaration);
                    foreach (var returnValue in walker.ReturnValues)
                    {
                        switch (returnValue)
                        {
                            case IdentifierNameSyntax { Identifier: { ValueText: { } valueText } }
                                when method.TryFindParameter(valueText, out _):
                            case ThisExpressionSyntax _:
                                continue;
                            default:
                                return false;
                        }
                    }

                    return true;
                }

                return false;
            }

            return true;
        }
    }
}
