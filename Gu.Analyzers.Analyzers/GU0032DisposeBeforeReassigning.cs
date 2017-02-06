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
            context.RegisterSyntaxNodeAction(HandleInvocation, SyntaxKind.InvocationExpression);
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (!Disposable.IsPotentiallyCreated(assignment.Right, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            var left = context.SemanticModel.GetSymbolSafe(assignment.Left, context.CancellationToken);
            if (left == KnownSymbol.SerialDisposable.Disposable)
            {
                return;
            }

            if (left is ILocalSymbol || left is IParameterSymbol)
            {
                if (!IsVariableAssignedBefore(left, assignment, context.SemanticModel, context.CancellationToken) ||
                    IsDisposedBefore(left, assignment, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
                return;
            }

            if (assignment.FirstAncestorOrSelf<MethodDeclarationSyntax>() != null)
            {
                if (IsDisposedBefore(left, assignment, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
            }

            if (assignment.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() != null)
            {
                if (!IsMemberInitialized(left, context.SemanticModel, context.CancellationToken) &&
                    !IsVariableAssignedBefore(left, assignment, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                if (IsDisposedBefore(left, assignment, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
            }
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation.ArgumentList == null ||
                invocation.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (!argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    continue;
                }

                if (!Disposable.IsPotentiallyCreated(argument.Expression, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                var argSymbol = context.SemanticModel.GetSymbolSafe(argument.Expression, context.CancellationToken);
                if (argSymbol == KnownSymbol.SerialDisposable.Disposable)
                {
                    return;
                }

                if (argSymbol is ILocalSymbol || argSymbol is IParameterSymbol)
                {
                    if (!IsVariableAssignedBefore(argSymbol, argument.Expression, context.SemanticModel, context.CancellationToken) ||
                        IsDisposedBefore(argSymbol, invocation, context.SemanticModel, context.CancellationToken))
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, argument.GetLocation()));
                    return;
                }

                if (invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>() != null)
                {
                    if (IsDisposedBefore(argSymbol, invocation, context.SemanticModel, context.CancellationToken))
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, argument.GetLocation()));
                }
            }
        }

        private static bool IsVariableAssignedBefore(ISymbol symbol, ExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var parameter = symbol as IParameterSymbol;
            if (parameter?.RefKind == RefKind.Ref)
            {
                return true;
            }

            VariableDeclaratorSyntax declarator;
            if (symbol.TryGetSingleDeclaration(cancellationToken, out declarator))
            {
                if (ReferenceEquals(declarator, assignment.FirstAncestorOrSelf<VariableDeclaratorSyntax>()))
                {
                    return false;
                }

                if (Disposable.IsPotentiallyCreated(declarator.Initializer?.Value, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            var statement = assignment.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null)
            {
                return false;
            }

            using (var pooled = AssignedValueWalker.AssignedValuesInType(symbol, semanticModel, cancellationToken))
            {
                foreach (var assignedValue in pooled.Item.AssignedValues)
                {
                    if (!assignedValue.IsBeforeInScope(statement))
                    {
                        continue;
                    }

                    if (Disposable.IsPotentiallyCreated(assignedValue, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsMemberInitialized(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var field = symbol as IFieldSymbol;
            if (field != null)
            {
                foreach (var declaration in field.Declarations(cancellationToken))
                {
                    if ((declaration as VariableDeclaratorSyntax)?.Initializer == null)
                    {
                        return false;
                    }
                }
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                foreach (var declaration in property.Declarations(cancellationToken))
                {
                    var propertyDeclaration = declaration as PropertyDeclarationSyntax;
                    if (propertyDeclaration == null)
                    {
                        continue;
                    }

                    if (propertyDeclaration.IsAutoProperty())
                    {
                        if (propertyDeclaration.Initializer == null)
                        {
                            return false;
                        }

                        continue;
                    }

                    AccessorDeclarationSyntax setter;
                    if(propertyDeclaration.TryGetSetAccessorDeclaration(out setter))
                    {
                        using (var pooled = AssignmentWalker.Create(setter))
                        {
                            foreach (var assignment in pooled.Item.Assignments)
                            {
                                var assignedSymbol = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken);
                                if (IsMemberInitialized(assignedSymbol, semanticModel, cancellationToken))
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool IsDisposedBefore(ISymbol symbol, ExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = InvocationWalker.Create(assignment.FirstAncestorOrSelf<MemberDeclarationSyntax>()))
            {
                foreach (var invocation in pooled.Item.Invocations)
                {
                    if (!invocation.IsBeforeInScope(assignment))
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