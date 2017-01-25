namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0033DontIgnoreReturnValueOfTypeIDisposable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0033";

        private const string Title = "Don't ignore returnvalue of type IDisposable.";

        private const string MessageFormat = "Don't ignore returnvalue of type IDisposable.";

        private const string Description = "Don't ignore returnvalue of type IDisposable.";

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
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            if (!Disposable.IsPotentialCreation(objectCreation, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            if (MustBeHandled(objectCreation, context.SemanticModel, context.CancellationToken))
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

            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation == null)
            {
                return;
            }

            var symbol = (IMethodSymbol)context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken);
            if (symbol == null ||
                symbol.ReturnsVoid)
            {
                return;
            }

            if (!Disposable.IsPotentialCreation(invocation, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            if (MustBeHandled(invocation, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static bool MustBeHandled(
            SyntaxNode node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (node.Parent is AnonymousFunctionExpressionSyntax ||
                node.Parent is UsingStatementSyntax)
            {
                return false;
            }

            if (node.Parent is StatementSyntax)
            {
                return !(node.Parent is ReturnStatementSyntax);
            }

            var argument = node.Parent as ArgumentSyntax;
            if (argument != null)
            {
                return !IsAssignedToDisposedFieldOrProperty(argument, semanticModel, cancellationToken);
            }

            return false;
        }

        private static bool IsAssignedToDisposedFieldOrProperty(
            ArgumentSyntax argument,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var objectCreation = argument.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();
            if (objectCreation != null)
            {
                var ctor = semanticModel.GetSymbolSafe(objectCreation, cancellationToken) as IMethodSymbol;
                return IsAssignedToDisposedFieldOrProperty(argument, ctor, semanticModel, cancellationToken);
            }

            var initializer = argument.FirstAncestorOrSelf<ConstructorInitializerSyntax>();
            if (initializer != null)
            {
                var ctor = semanticModel.GetSymbolSafe(initializer, cancellationToken) as IMethodSymbol;
                return IsAssignedToDisposedFieldOrProperty(argument, ctor, semanticModel, cancellationToken);
            }

            return false;
        }

        private static bool IsAssignedToDisposedFieldOrProperty(ArgumentSyntax argument, IMethodSymbol method, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (method == null)
            {
                return false;
            }

            if (method.ContainingType == KnownSymbol.SerialDisposable ||
                method.ContainingType.Is(KnownSymbol.StreamReader))
            {
                return true;
            }

            foreach (var declaration in method.Declarations(cancellationToken))
            {
                var methodDeclaration = declaration as BaseMethodDeclarationSyntax;
                if (methodDeclaration == null)
                {
                    continue;
                }

                ParameterSyntax paremeter;
                if (!methodDeclaration.TryGetMatchingParameter(argument, out paremeter))
                {
                    continue;
                }

                var parameterSymbol = semanticModel.GetDeclaredSymbolSafe(paremeter, cancellationToken);
                AssignmentExpressionSyntax assignment;
                if (methodDeclaration.Body.TryGetAssignment(parameterSymbol, semanticModel, cancellationToken, out assignment))
                {
                    var left = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken);
                    if (left is IFieldSymbol ||
                        left is IPropertySymbol)
                    {
                        return Disposable.IsMemberDisposed(left, semanticModel, cancellationToken);
                    }
                }

                var ctor = declaration as ConstructorDeclarationSyntax;
                if (ctor?.Initializer != null)
                {
                    foreach (var arg in ctor.Initializer.ArgumentList.Arguments)
                    {
                        var argSymbol = semanticModel.GetSymbolSafe(arg.Expression, cancellationToken);
                        if (parameterSymbol.Equals(argSymbol))
                        {
                            var chained = semanticModel.GetSymbolSafe(ctor.Initializer, cancellationToken) as IMethodSymbol;
                            return IsAssignedToDisposedFieldOrProperty(arg, chained, semanticModel, cancellationToken);
                        }
                    }
                }
            }

            return false;
        }
    }
}