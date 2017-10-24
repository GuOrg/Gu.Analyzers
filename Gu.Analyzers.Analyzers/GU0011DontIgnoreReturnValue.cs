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
            title: "Don't ignore the returnvalue.",
            messageFormat: "Don't ignore the returnvalue.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't ignore the returnvalue.",
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
                IsIgnored(objectCreation) &&
                !CanIgnore(objectCreation))
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

        private static bool CanIgnore(ObjectCreationExpressionSyntax invocation)
        {
            if (invocation.Parent is ExpressionStatementSyntax &&
                invocation.Parent.Parent is BlockSyntax)
            {
                return false;
            }

            return true;
        }

        private static bool CanIgnore(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var method = (IMethodSymbol)semanticModel.GetSymbolSafe(invocation, cancellationToken);
            if (method == null || method.ReturnsVoid)
            {
                return true;
            }

            if (!(invocation.Parent is StatementSyntax))
            {
                return true;
            }

            if (invocation.Parent is ExpressionStatementSyntax &&
                invocation.Parent.Parent is BlockSyntax)
            {
                if (method == KnownSymbol.StringBuilder.Append ||
                    method == KnownSymbol.StringBuilder.AppendLine ||
                    method == KnownSymbol.StringBuilder.AppendFormat ||
                    method == KnownSymbol.IList.Add ||
                    method == KnownSymbol.IList.Remove)
                {
                    return true;
                }

                if (method.TryGetSingleDeclaration(cancellationToken, out MethodDeclarationSyntax declaration))
                {
                    using (var walker = ReturnValueWalker.Borrow(declaration, Search.Recursive, semanticModel, cancellationToken))
                    {
                        if (method.IsExtensionMethod)
                        {
                            var identifier = declaration.ParameterList.Parameters[0].Identifier;
                            foreach (var returnValue in walker)
                            {
                                if ((returnValue as IdentifierNameSyntax)?.Identifier.ValueText != identifier.ValueText)
                                {
                                    return false;
                                }
                            }

                            return true;
                        }

                        foreach (var returnValue in walker)
                        {
                            if (!returnValue.IsKind(SyntaxKind.ThisExpression))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }

                return method.ReturnsVoid;
            }

            return true;
        }

        private static bool IsIgnored(SyntaxNode node)
        {
            if (node.Parent is StatementSyntax)
            {
                return !(node.Parent is ReturnStatementSyntax);
            }

            var argument = node.FirstAncestorOrSelf<ArgumentSyntax>();
            if (argument != null)
            {
                var objectCreation = argument.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();
                if ((objectCreation?.Type as IdentifierNameSyntax)?.Identifier.ValueText.Contains("Reader") == true)
                {
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}