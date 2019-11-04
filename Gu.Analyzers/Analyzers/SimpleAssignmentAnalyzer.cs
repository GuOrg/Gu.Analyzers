namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class SimpleAssignmentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0010DoNotAssignSameValue,
            Descriptors.GU0012NullCheckParameter,
            Descriptors.GU0015DoNotAssignMoreThanOnce);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.SimpleAssignmentExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is AssignmentExpressionSyntax assignment &&
                context.SemanticModel.TryGetSymbol(assignment.Left, context.CancellationToken, out ISymbol? left))
            {
                if (context.SemanticModel.TryGetSymbol(assignment.Right, context.CancellationToken, out ISymbol? right) &&
                    AreSame(assignment.Left, assignment.Right) &&
                    assignment.FirstAncestorOrSelf<InitializerExpressionSyntax>() is null &&
                    left.Equals(right))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0010DoNotAssignSameValue, assignment.GetLocation()));
                }

                if (assignment.Right is IdentifierNameSyntax identifier &&
                    context.ContainingSymbol is IMethodSymbol method &&
                    method.DeclaredAccessibility.IsEither(Accessibility.Internal, Accessibility.Protected, Accessibility.Public) &&
                    method.Parameters.TryFirst(x => x.Name == identifier.Identifier.ValueText, out var parameter) &&
                    parameter.Type.IsReferenceType &&
                    !parameter.HasExplicitDefaultValue &&
                    assignment.TryFirstAncestor(out BaseMethodDeclarationSyntax? containingMethod) &&
                    !NullCheck.IsChecked(parameter, containingMethod, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0012NullCheckParameter, assignment.Right.GetLocation()));
                }

                if (IsRedundantAssignment(left, assignment, context))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0015DoNotAssignMoreThanOnce, assignment.GetLocation()));
                }
            }
        }

        private static bool AreSame(ExpressionSyntax left, ExpressionSyntax right)
        {
            if (TryGetIdentifierName(left, out var leftName) ^ TryGetIdentifierName(right, out var rightName))
            {
                return false;
            }

            if (leftName != null)
            {
                return leftName.Identifier.ValueText == rightName.Identifier.ValueText;
            }

            return left is MemberAccessExpressionSyntax leftMember &&
                   right is MemberAccessExpressionSyntax rightMember &&
                   AreSame(leftMember.Name, rightMember.Name) &&
                   AreSame(leftMember.Expression, rightMember.Expression);
        }

        private static bool TryGetIdentifierName(ExpressionSyntax expression, [NotNullWhen(true)]out IdentifierNameSyntax? result)
        {
            switch (expression)
            {
                case IdentifierNameSyntax identifierName:
                    result = identifierName;
                    return true;
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Expression is ThisExpressionSyntax:
                    return TryGetIdentifierName(memberAccess.Name, out result);
                default:
                    result = null;
                    return false;
            }
        }

        private static bool IsRedundantAssignment(ISymbol left, AssignmentExpressionSyntax assignment, SyntaxNodeAnalysisContext context)
        {
            if (left is IDiscardSymbol ||
                assignment.TryFirstAncestor<ObjectCreationExpressionSyntax>(out _))
            {
                return false;
            }

            if (assignment.TryFirstAncestor<MemberDeclarationSyntax>(out var member))
            {
                if (!(member is ConstructorDeclarationSyntax) &&
                    context.SemanticModel.TryGetType(assignment.Left, context.CancellationToken, out var type) &&
                    FieldOrProperty.TryCreate(left, out _))
                {
                    if (type == KnownSymbol.Boolean ||
                        type.TypeKind == TypeKind.Enum)
                    {
                        return false;
                    }
                }

                using (var walker = AssignmentExecutionWalker.For(left, member, SearchScope.Member, context.SemanticModel, context.CancellationToken))
                {
                    foreach (var candidate in walker.Assignments)
                    {
                        if (candidate == assignment)
                        {
                            continue;
                        }

                        if (!MemberPath.Equals(candidate.Left, assignment.Left))
                        {
                            continue;
                        }

                        if (candidate.IsExecutedBefore(assignment) == ExecutedBefore.Yes)
                        {
                            if (left is IParameterSymbol parameter &&
                                parameter.RefKind == RefKind.Out &&
                                assignment.TryFirstAncestor<BlockSyntax>(out var assignmentBlock) &&
                                candidate.TryFirstAncestor<BlockSyntax>(out var candidateBlock) &&
                                (candidateBlock.Contains(assignmentBlock) ||
                                 candidateBlock.Statements.Last() is ReturnStatementSyntax))
                            {
                                return false;
                            }

                            using (var nameWalker = IdentifierNameWalker.Borrow(assignment.Right))
                            {
                                foreach (var name in nameWalker.IdentifierNames)
                                {
                                    if (left.Name == name.Identifier.ValueText &&
                                        context.SemanticModel.TryGetSymbol(name, context.CancellationToken, out ISymbol? symbol) &&
                                        symbol.Equals(left))
                                    {
                                        return false;
                                    }
                                }
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
