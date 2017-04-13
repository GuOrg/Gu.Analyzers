namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0010DoNotAssignSameValue : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0010";
        private const string Title = "Assigning same value.";
        private const string MessageFormat = "Assigning made to same, did you mean to assign something else?";
        private const string Description = "Assigning same value does not make sense and is sign of a bug.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: Description,
            helpLinkUri: HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

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

            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (assignment.IsMissing)
            {
                return;
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

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
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