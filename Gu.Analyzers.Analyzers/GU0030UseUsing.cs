namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0030UseUsing : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0030";
        private const string Title = "Use using.";
        private const string MessageFormat = "Use using.";
        private const string Description = "Use using.";
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
            context.RegisterSyntaxNodeAction(HandleDeclaration, SyntaxKind.VariableDeclaration);
        }

        private static void HandleDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var variableDeclaration = (VariableDeclarationSyntax)context.Node;
            VariableDeclaratorSyntax declarator;
            if (!variableDeclaration.Variables.TryGetSingle(out declarator) ||
                declarator.Initializer == null)
            {
                return;
            }

            var symbol = context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken) as ILocalSymbol;
            if (symbol == null)
            {
                return;
            }

            var isCreation = Disposable.IsCreation(declarator.Initializer.Value, context.SemanticModel, context.CancellationToken);
            if (isCreation == Result.Yes || isCreation == Result.Maybe)
            {
                if (variableDeclaration.Parent is UsingStatementSyntax ||
                    variableDeclaration.Parent is AnonymousFunctionExpressionSyntax)
                {
                    return;
                }

                if (IsReturned(declarator, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                if (IsAssignedToFieldOrProperty(declarator, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                if (IsAddedToFieldOrProperty(declarator, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                if (IsDisposedAfter(symbol, declarator.Initializer.Value, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, variableDeclaration.GetLocation()));
            }
        }

        private static bool IsReturned(VariableDeclaratorSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (variable == null)
            {
                return false;
            }

            var symbol = semanticModel.GetDeclaredSymbolSafe(variable, cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            var block = variable.FirstAncestorOrSelf<BlockSyntax>();
            ExpressionSyntax returnValue = null;
            if (block?.TryGetReturnExpression(out returnValue) == true)
            {
                var returned = semanticModel.GetSymbolSafe(returnValue, cancellationToken);
                if (symbol.Equals(returned))
                {
                    return true;
                }

                var objectCreation = returnValue as ObjectCreationExpressionSyntax;
                if (objectCreation?.ArgumentList != null)
                {
                    foreach (var argument in objectCreation.ArgumentList.Arguments)
                    {
                        var arg = semanticModel.GetSymbolSafe(argument.Expression, cancellationToken);
                        if (symbol.Equals(arg))
                        {
                            return true;
                        }
                    }
                }

                if (objectCreation?.Initializer != null)
                {
                    foreach (var argument in objectCreation.Initializer.Expressions)
                    {
                        var arg = semanticModel.GetSymbolSafe(argument, cancellationToken);
                        if (symbol.Equals(arg))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsAssignedToFieldOrProperty(VariableDeclaratorSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (variable == null)
            {
                return false;
            }

            var symbol = semanticModel.GetDeclaredSymbolSafe(variable, cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            var block = variable.FirstAncestorOrSelf<BlockSyntax>();
            AssignmentExpressionSyntax assignment = null;
            if (block?.TryGetAssignment(symbol, semanticModel, cancellationToken, out assignment) == true)
            {
                var left = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken) ??
                           semanticModel.GetSymbolSafe((assignment.Left as ElementAccessExpressionSyntax)?.Expression, cancellationToken);
                return left is IFieldSymbol || left is IPropertySymbol || left is ILocalSymbol || left is IParameterSymbol;
            }

            return false;
        }

        private static bool IsAddedToFieldOrProperty(VariableDeclaratorSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (variable == null)
            {
                return false;
            }

            var symbol = semanticModel.GetDeclaredSymbolSafe(variable, cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            var block = variable.FirstAncestorOrSelf<BlockSyntax>();
            using (var pooledInvocations = InvocationWalker.Create(block))
            {
                foreach (var invocation in pooledInvocations.Item.Invocations)
                {
                    var method = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
                    if (method?.Name == "Add")
                    {
                        using (var pooledIdentifiers = IdentifierNameWalker.Create(invocation.ArgumentList))
                        {
                            foreach (var identifierName in pooledIdentifiers.Item.IdentifierNames)
                            {
                                var argSymbol = semanticModel.GetSymbolSafe(identifierName, cancellationToken);
                                if (symbol.Equals(argSymbol))
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

        private static bool IsDisposedAfter(ISymbol symbol, ExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = InvocationWalker.Create(assignment.FirstAncestorOrSelf<MemberDeclarationSyntax>()))
            {
                foreach (var invocation in pooled.Item.Invocations)
                {
                    if (!IsAfter(invocation, assignment))
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

        private static bool IsAfter(SyntaxNode node, SyntaxNode other)
        {
            var statement = node?.FirstAncestorOrSelf<StatementSyntax>();
            var otherStatement = other?.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null ||
                otherStatement == null)
            {
                return false;
            }

            if (statement.SpanStart <= otherStatement.SpanStart)
            {
                return false;
            }

            var block = node.FirstAncestor<BlockSyntax>();
            var otherblock = other.FirstAncestor<BlockSyntax>();

            if (block == null || otherblock == null)
            {
                return false;
            }

            return ReferenceEquals(block, otherblock);
        }
    }
}