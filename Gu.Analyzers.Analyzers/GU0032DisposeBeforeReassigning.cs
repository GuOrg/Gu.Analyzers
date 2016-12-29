namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0032DisposeBeforeReassigning : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0032";
        private const string Title = "Dispose before re-assigning.";
        private const string MessageFormat = "Dispose before re-assigning.";
        private const string Description = "Dispose before re-assigning.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
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
            context.RegisterSyntaxNodeAction(HandleAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (!Disposable.IsPotentialCreation(assignment.Right, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolSafe(assignment.Left, context.CancellationToken);

            var localSymbol = symbol as ILocalSymbol;
            if (localSymbol != null)
            {
                if (!IsVariableAssignedBefore(localSymbol, assignment, context.SemanticModel, context.CancellationToken) ||
                    IsDisposedBeforeAssignment(symbol, assignment))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
                return;
            }

            if (assignment.FirstAncestorOrSelf<MethodDeclarationSyntax>() != null)
            {
                if (IsDisposedBeforeAssignment(symbol, assignment))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
            }
        }

        private static bool IsVariableAssignedBefore(ILocalSymbol symbol, AssignmentExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            VariableDeclaratorSyntax declarator;
            if (symbol.TryGetSingleDeclaration(cancellationToken, out declarator))
            {
                if (Disposable.IsPotentialCreation(declarator.Initializer?.Value, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            using (var pooled = AssignmentWalker.Create(assignment.FirstAncestorOrSelf<MemberDeclarationSyntax>()))
            {
                foreach (var previousAssignment in pooled.Item.Assignments)
                {
                    if (previousAssignment.SpanStart >= assignment.SpanStart)
                    {
                        return false;
                    }

                    if (previousAssignment.Left == assignment.Left)
                    {
                        if (Disposable.IsPotentialCreation(assignment.Right, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsDisposedBeforeAssignment(ISymbol symbol, AssignmentExpressionSyntax assignment)
        {
            using (var pooled = InvocationWalker.Create(assignment.FirstAncestorOrSelf<MemberDeclarationSyntax>()))
            {
                foreach (var invocation in pooled.Item.Invocations)
                {
                    if (invocation.SpanStart > assignment.SpanStart)
                    {
                        break;
                    }

                    var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
                    if (statement != null)
                    {
                        using (var pooledStatement = IdentifierNameWalker.Create(statement))
                        {
                            foreach (var identifierName in pooledStatement.Item.IdentifierNames)
                            {
                                if (identifierName?.Identifier.ValueText == symbol.Name)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}