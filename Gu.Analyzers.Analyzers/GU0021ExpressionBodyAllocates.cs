namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0021ExpressionBodyAllocates : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0021";
        private const string Title = "Expression body allocates reference type.";
        private const string MessageFormat = "Expression body allocates reference type.";
        private const string Description = "Expression body allocates reference type.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.Correctness,
            DiagnosticSeverity.Hidden,
            AnalyzerConstants.EnabledByDefault,
            Description,
            HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ArrowExpressionClause);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            var arrow = (ArrowExpressionClauseSyntax)context.Node;
            var objectCreation = arrow.Expression as ObjectCreationExpressionSyntax;
            if (objectCreation == null)
            {
                return;
            }

            var type = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken).Type;
            if (!type.IsReferenceType)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, arrow.GetLocation()));
        }
    }
}