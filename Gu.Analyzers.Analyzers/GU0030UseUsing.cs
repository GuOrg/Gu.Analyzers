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
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.VariableDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var variableDeclaration = (VariableDeclarationSyntax)context.Node;
            VariableDeclaratorSyntax declarator;
            if (!variableDeclaration.Variables.TryGetSingle(out declarator))
            {
                return;
            }

            var symbol = context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken) as ILocalSymbol;
            if (symbol == null)
            {
                return;
            }

            if (Disposable.IsAssignableTo(symbol.Type) && declarator.Initializer != null)
            {
                if (Disposable.IsPotentialCreation(declarator.Initializer.Value, context.SemanticModel, context.CancellationToken))
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

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, variableDeclaration.GetLocation()));
                }
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
                if (objectCreation != null)
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
                var left = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken);
                return left is IFieldSymbol || left is IPropertySymbol;
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
    }
}