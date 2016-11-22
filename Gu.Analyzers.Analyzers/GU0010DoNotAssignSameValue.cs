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

            var left = context.SemanticModel.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol;
            if (left == null)
            {
                return;
            }

            var right = context.SemanticModel.GetSymbolInfo(assignment.Right, context.CancellationToken).Symbol;
            if (right == null)
            {
                return;
            }

            if (ReferenceEquals(left, right))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
            }
        }
    }
}