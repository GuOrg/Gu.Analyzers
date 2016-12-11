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
        private const string Title = "Don't ignore the returnvalue.";
        private const string MessageFormat = "Don't ignore returnvalue.";
        private const string Description = "Don't ignore the returnvalue.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: Description,
            helpLinkUri: HelpLink);

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
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            if (objectCreation == null || CanIgnore(objectCreation))
            {
                return;
            }

            if (IsIgnored(objectCreation))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation == null || CanIgnore(invocation, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            if (IsIgnored(invocation))
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
            if (!(invocation.Parent is StatementSyntax))
            {
                return true;
            }

            if (invocation.Parent is ExpressionStatementSyntax &&
                invocation.Parent.Parent is BlockSyntax)
            {
                var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(invocation, cancellationToken)
                                                          .Symbol;
                if (symbol == null)
                {
                    return true;
                }

                if (symbol == KnownSymbol.StringBuilder.AppendLine ||
                    symbol == KnownSymbol.StringBuilder.Append)
                {
                    return true;
                }

                MethodDeclarationSyntax declaration;
                if (symbol.TryGetSingleDeclaration(cancellationToken, out declaration))
                {
                    using (var walker = ReturnExpressionsWalker.Create(declaration))
                    {
                        if (symbol.IsExtensionMethod)
                        {
                            var identifier = declaration.ParameterList.Parameters[0].Identifier;
                            foreach (var returnValue in walker.ReturnValues)
                            {
                                if ((returnValue as IdentifierNameSyntax)?.Identifier.ValueText != identifier.ValueText)
                                {
                                    return false;
                                }
                            }

                            return true;
                        }
                        else
                        {
                            foreach (var returnValue in walker.ReturnValues)
                            {
                                if (!returnValue.IsKind(SyntaxKind.ThisExpression))
                                {
                                    return false;
                                }
                            }

                            return true;
                        }
                    }
                }

                return symbol.ReturnsVoid;
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