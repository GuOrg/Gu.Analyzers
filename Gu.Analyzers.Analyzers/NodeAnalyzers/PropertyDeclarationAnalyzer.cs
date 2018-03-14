namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class PropertyDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(GU0021CalculatedPropertyAllocates.Descriptor);

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
                if (property.Type.IsReferenceType)
                {
                    if (propertyDeclaration.ExpressionBody is ArrowExpressionClauseSyntax expressionBody &&
                        expressionBody.Expression is ObjectCreationExpressionSyntax)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GU0021CalculatedPropertyAllocates.Descriptor, expressionBody.GetLocation()));
                    }
                    else if (propertyDeclaration.TryGetGetter(out var getter))
                    {
                        using (var walker = ReturnValueWalker.Borrow(getter, Search.Recursive, context.SemanticModel, context.CancellationToken))
                        {
                            if (walker.TrySingle(out var returnValue) &&
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
                        }
                    }
                }
            }
        }
    }
}
