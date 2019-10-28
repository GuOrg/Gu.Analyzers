namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class PropertyDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0008AvoidRelayProperties,
            Descriptors.GU0021CalculatedPropertyAllocates);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.PropertyDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is PropertyDeclarationSyntax propertyDeclaration &&
                context.ContainingSymbol is IPropertySymbol property &&
                property.GetMethod != null &&
                ReturnValueWalker.TrySingle(propertyDeclaration, out var returnValue))
            {
                if (property.Type.IsReferenceType &&
                    property.SetMethod == null &&
                    returnValue is ObjectCreationExpressionSyntax)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0021CalculatedPropertyAllocates, returnValue.GetLocation()));
                }
                else if (returnValue is MemberAccessExpressionSyntax memberAccess &&
                         IsRelayReturn(memberAccess, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0008AvoidRelayProperties, memberAccess.GetLocation()));
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

            if (semanticModel.TryGetSymbol(memberAccess.Expression, cancellationToken, out ISymbol? member) &&
                FieldOrProperty.TryCreate(member, out FieldOrProperty fieldOrProperty) &&
                memberAccess.TryFirstAncestor<TypeDeclarationSyntax>(out var typeDeclaration) &&
                !IsInjected(fieldOrProperty, typeDeclaration, semanticModel, cancellationToken))
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

        private static bool IsInjected(FieldOrProperty member, TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var walker = AssignmentExecutionWalker.For(member.Symbol, typeDeclaration, SearchScope.Instance, semanticModel, cancellationToken))
            {
                foreach (var assignment in walker.Assignments)
                {
                    if (assignment.TryFirstAncestorOrSelf<ConstructorDeclarationSyntax>(out _) &&
                        semanticModel.GetSymbolSafe(assignment.Right, cancellationToken) is IParameterSymbol)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
