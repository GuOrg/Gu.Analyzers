namespace Gu.Analyzers
{
    using System.Collections.Immutable;
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
            GU0010DoNotAssignSameValue.Descriptor,
            GU0012NullCheckParameter.Descriptor,
            GU0015DontAssignMoreThanOnce.Descriptor);

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

            if (context.Node is AssignmentExpressionSyntax assignment &&
                context.SemanticModel.TryGetSymbol(assignment.Left, context.CancellationToken, out ISymbol left) &&
                context.SemanticModel.TryGetSymbol(assignment.Right, context.CancellationToken, out ISymbol right))
            {
                if (AreSame(assignment.Left, assignment.Right) &&
                    assignment.FirstAncestorOrSelf<InitializerExpressionSyntax>() == null &&
                    left.Equals(right))
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0010DoNotAssignSameValue.Descriptor, assignment.GetLocation()));
                }

                if (assignment.Right is IdentifierNameSyntax identifier &&
                    context.ContainingSymbol is IMethodSymbol method &&
                    method.DeclaredAccessibility.IsEither(Accessibility.Internal, Accessibility.Protected, Accessibility.Public) &&
                    method.Parameters.TryFirst(x => x.Name == identifier.Identifier.ValueText, out var parameter) &&
                    parameter.Type.IsReferenceType &&
                    !parameter.HasExplicitDefaultValue &&
                    !NullCheck.IsChecked(parameter, assignment.FirstAncestor<BaseMethodDeclarationSyntax>(), context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0012NullCheckParameter.Descriptor, assignment.Right.GetLocation()));
                }

                if (IsAssignedBefore(left, assignment, context))
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0015DontAssignMoreThanOnce.Descriptor, assignment.GetLocation()));
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

        private static bool TryGetIdentifierName(ExpressionSyntax expression, out IdentifierNameSyntax result)
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

        private static bool IsAssignedBefore(ISymbol left, AssignmentExpressionSyntax assignment, SyntaxNodeAnalysisContext context)
        {
            if (assignment.TryFirstAncestor<MemberDeclarationSyntax>(out var member))
            {
                using (var walker = AssignmentExecutionWalker.For(left, member, Scope.Member, context.SemanticModel, context.CancellationToken))
                {
                    foreach (var candaidate in walker.Assignments)
                    {
                        if (candaidate == assignment)
                        {
                            continue;
                        }

                        if (candaidate.IsExecutedBefore(assignment) == true)
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
