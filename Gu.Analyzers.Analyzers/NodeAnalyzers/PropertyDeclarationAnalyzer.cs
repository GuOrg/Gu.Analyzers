namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class PropertyDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GU0008AvoidRelayProperties.Descriptor,
            GU0021CalculatedPropertyAllocates.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.PropertyDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is PropertyDeclarationSyntax propertyDeclaration &&
                context.ContainingSymbol is IPropertySymbol property)
            {
                {
                    if (propertyDeclaration.ExpressionBody is ArrowExpressionClauseSyntax expressionBody)
                    {
                        if (property.Type.IsReferenceType &&
                            expressionBody.Expression is ObjectCreationExpressionSyntax)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(GU0021CalculatedPropertyAllocates.Descriptor, expressionBody.GetLocation()));
                        }
                        else if (expressionBody.Expression is MemberAccessExpressionSyntax memberAccess &&
                                 IsRelayReturn(memberAccess, context.SemanticModel, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(GU0008AvoidRelayProperties.Descriptor, memberAccess.GetLocation()));
                        }
                    }
                    else if (propertyDeclaration.TryGetGetter(out var getter))
                    {
                        using (var walker = ReturnValueWalker.Borrow(getter, Search.Recursive, context.SemanticModel, context.CancellationToken))
                        {
                            if (walker.TrySingle(out var returnValue))
                            {
                                if (property.Type.IsReferenceType &&
                                    returnValue is ObjectCreationExpressionSyntax)
                                {
                                    if (getter.Contains(returnValue) &&
                                        returnValue.FirstAncestor<ReturnStatementSyntax>() is ReturnStatementSyntax returnStatement)
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(GU0021CalculatedPropertyAllocates.Descriptor, returnStatement.GetLocation()));
                                    }
                                    else
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(GU0021CalculatedPropertyAllocates.Descriptor, getter.GetLocation()));
                                    }
                                }
                                else if (returnValue is MemberAccessExpressionSyntax memberAccess &&
                                         IsRelayReturn(memberAccess, context.SemanticModel, context.CancellationToken))
                                {
                                    if (getter.Contains(returnValue) &&
                                        returnValue.FirstAncestor<ReturnStatementSyntax>() is ReturnStatementSyntax returnStatement)
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(GU0008AvoidRelayProperties.Descriptor, returnStatement.GetLocation()));
                                    }
                                    else
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(GU0008AvoidRelayProperties.Descriptor, getter.GetLocation()));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsRelayReturn(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (memberAccess == null ||
                !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression) ||
                memberAccess.Expression is InstanceExpressionSyntax ||
                memberAccess.Expression == null)
            {
                return false;
            }

            var member = semanticModel.GetSymbolSafe(memberAccess.Expression, cancellationToken);
            if (member == null ||
                !IsInjected(member, semanticModel, cancellationToken))
            {
                return false;
            }

            if (memberAccess.Expression is IdentifierNameSyntax &&
                memberAccess.Name is IdentifierNameSyntax)
            {
                return true;
            }

            if (memberAccess.Expression is MemberAccessExpressionSyntax &&
                memberAccess.Name is IdentifierNameSyntax)
            {
                return true;
            }

            return false;
        }

        private static bool IsInjected(ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (member is IFieldSymbol field)
            {
                using (var walker = AssignedValueWalker.Borrow(field, semanticModel, cancellationToken))
                {
                    foreach (var assignedValue in walker)
                    {
                        if (assignedValue.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() == null)
                        {
                            continue;
                        }

                        if (semanticModel.GetSymbolSafe(assignedValue, cancellationToken) is IParameterSymbol)
                        {
                            return true;
                        }
                    }
                }
            }

            if (member is IPropertySymbol property)
            {
                using (var walker = AssignedValueWalker.Borrow(property, semanticModel, cancellationToken))
                {
                    foreach (var assignedValue in walker)
                    {
                        if (assignedValue.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() == null)
                        {
                            continue;
                        }

                        if (semanticModel.GetSymbolSafe(assignedValue, cancellationToken) is IParameterSymbol)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
