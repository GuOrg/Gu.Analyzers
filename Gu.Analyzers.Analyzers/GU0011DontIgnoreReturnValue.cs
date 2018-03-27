namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0011DontIgnoreReturnValue : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0011";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't ignore the return value.",
            messageFormat: "Don't ignore the return value.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't ignore the return value.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(HandleInvocation, SyntaxKind.InvocationExpression);
        }

        private static void HandleCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                IsIgnored(objectCreation))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is InvocationExpressionSyntax invocation &&
                IsIgnored(invocation) &&
                !CanIgnore(invocation, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static bool IsIgnored(SyntaxNode node)
        {
            return node.Parent is ExpressionStatementSyntax expressionStatement &&
                   expressionStatement.Parent is BlockSyntax;
        }

        private static bool CanIgnore(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (semanticModel.GetSymbolSafe(invocation, cancellationToken) is IMethodSymbol method)
            {
                if (method.ReturnsVoid ||
                    method.ReturnType == KnownSymbol.MoqIReturnsResult)
                {
                    return true;
                }

                if (ReferenceEquals(method.ContainingType, method.ReturnType) &&
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

                if (method.TrySingleDeclaration(cancellationToken, out MethodDeclarationSyntax declaration))
                {
                    using (var walker = ReturnValueWalker.Borrow(declaration, Search.Recursive, semanticModel, cancellationToken))
                    {
                        foreach (var returnValue in walker)
                        {
                            if (returnValue is IdentifierNameSyntax identifierName &&
                                method.Parameters.TryFirst(x => x.Name == identifierName.Identifier.ValueText, out _))
                            {
                                return true;
                            }

                            if (returnValue is InstanceExpressionSyntax)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }
            }

            return false;
        }
    }
}
