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
        private const string MessageFormat = "Assigning same value.";
        private const string Description = "Assigning same value does not make sense.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.Correctness,
            DiagnosticSeverity.Error,
            AnalyzerConstants.EnabledByDefault,
            Description,
            HelpLink);

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
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (assignment.IsMissing)
            {
                return;
            }

            IdentifierNameSyntax left;
            IdentifierNameSyntax right;
            if (!TryGetIdentifier(assignment.Left, out left) ||
                !TryGetIdentifier(assignment.Right, out right))
            {
                return;
            }

            if (left.Identifier.ValueText == right.Identifier.ValueText)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
            }
        }

        private static bool TryGetIdentifier(ExpressionSyntax expression, out IdentifierNameSyntax result)
        {
            result = expression as IdentifierNameSyntax;
            if (result != null)
            {
                return true;
            }

            var member = expression as MemberAccessExpressionSyntax;
            if (member?.Expression is ThisExpressionSyntax)
            {
                return TryGetIdentifier(member.Name, out result);
            }

            return false;
        }
    }
}