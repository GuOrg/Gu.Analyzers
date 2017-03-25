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
            context.RegisterSyntaxNodeAction(HandleArgument, SyntaxKind.Argument);
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (Disposable.IsCreation(assignment.Right, context.SemanticModel, context.CancellationToken)
                          .IsEither(Result.No, Result.Unknown))
            {
                return;
            }

            if (Disposable.IsAssignedWithCreated(assignment.Left, context.SemanticModel, context.CancellationToken, out ISymbol assignedSymbol)
                          .IsEither(Result.No, Result.Unknown))
            {
                return;
            }

            if (assignedSymbol == KnownSymbol.SerialDisposable.Disposable)
            {
                return;
            }

            if (IsDisposedBefore(assignedSymbol, assignment, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
        }

        private static void HandleArgument(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var argument = (ArgumentSyntax)context.Node;
            if (argument.RefOrOutKeyword.IsKind(SyntaxKind.None))
            {
                return;
            }

            var invocation = argument.FirstAncestor<InvocationExpressionSyntax>();
            if (invocation == null)
            {
                return;
            }

            var method = context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken);
            if (method == null ||
                method.DeclaredAccessibility == Accessibility.Private ||
                method.DeclaringSyntaxReferences.Length == 0)
            {
                return;
            }

            if (Disposable.IsAssignedWithCreated(argument.Expression, context.SemanticModel, context.CancellationToken, out ISymbol assignedSymbol)
                          .IsEither(Result.No, Result.Unknown))
            {
                return;
            }

            if (IsDisposedBefore(assignedSymbol, argument.Expression, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, argument.GetLocation()));
        }

        private static bool IsDisposedBefore(ISymbol symbol, ExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = InvocationWalker.Create(assignment.FirstAncestorOrSelf<MemberDeclarationSyntax>()))
            {
                foreach (var invocation in pooled.Item.Invocations)
                {
                    if (invocation.IsBeforeInScope(assignment) != Result.Yes)
                    {
                        continue;
                    }

                    var invokedSymbol = semanticModel.GetSymbolSafe(invocation, cancellationToken);
                    if (invokedSymbol?.Name != "Dispose")
                    {
                        continue;
                    }

                    var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
                    if (statement != null)
                    {
                        using (var pooledNames = IdentifierNameWalker.Create(statement))
                        {
                            foreach (var identifierName in pooledNames.Item.IdentifierNames)
                            {
                                var otherSymbol = semanticModel.GetSymbolSafe(identifierName, cancellationToken);
                                if (symbol.Equals(otherSymbol))
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