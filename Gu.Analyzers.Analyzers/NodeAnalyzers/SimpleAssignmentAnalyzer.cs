namespace Gu.Analyzers
{
    using System.Collections.Immutable;
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
            GU0012NullCheckParameter.Descriptor);

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

            if (context.Node is AssignmentExpressionSyntax assignment)
            {
                if (assignment.Right is IdentifierNameSyntax identifier &&
                    context.ContainingSymbol is IMethodSymbol method &&
                    method.DeclaredAccessibility.IsEither(Accessibility.Internal, Accessibility.Public) &&
                    method.Parameters.TryFirst(x => x.Name == identifier.Identifier.ValueText, out var parameter) &&
                    parameter.Type.IsReferenceType &&
                    !parameter.HasExplicitDefaultValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0012NullCheckParameter.Descriptor, assignment.Right.GetLocation()));
                }

                if (AreSame(assignment.Left, assignment.Right))
                {
                    if (assignment.FirstAncestorOrSelf<InitializerExpressionSyntax>() != null)
                    {
                        return;
                    }

                    var left = context.SemanticModel.GetSymbolSafe(assignment.Left, context.CancellationToken);
                    var right = context.SemanticModel.GetSymbolSafe(assignment.Right, context.CancellationToken);
                    if (!ReferenceEquals(left, right))
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(GU0010DoNotAssignSameValue.Descriptor, assignment.GetLocation()));
                }
            }
        }

        private static bool AreSame(ExpressionSyntax left, ExpressionSyntax right)
        {
            if (TryGetIdentifierName(left, out IdentifierNameSyntax leftName) ^ TryGetIdentifierName(right, out IdentifierNameSyntax rightName))
            {
                return false;
            }

            if (leftName != null)
            {
                return leftName.Identifier.ValueText == rightName.Identifier.ValueText;
            }

            var leftMember = left as MemberAccessExpressionSyntax;
            var rightMember = right as MemberAccessExpressionSyntax;
            if (leftMember == null || rightMember == null)
            {
                return false;
            }

            return AreSame(leftMember.Name, rightMember.Name) && AreSame(leftMember.Expression, rightMember.Expression);
        }

        private static bool TryGetIdentifierName(ExpressionSyntax expression, out IdentifierNameSyntax result)
        {
            result = expression as IdentifierNameSyntax;
            if (result != null)
            {
                return true;
            }

            var memberAccess = expression as MemberAccessExpressionSyntax;
            if (memberAccess?.Expression is ThisExpressionSyntax)
            {
                return TryGetIdentifierName(memberAccess.Name, out result);
            }

            return false;
        }
    }
}
