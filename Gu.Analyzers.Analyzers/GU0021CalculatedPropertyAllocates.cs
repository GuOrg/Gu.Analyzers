namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0021CalculatedPropertyAllocates : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0021";
        private const string Title = "Calculated property allocates reference type.";
        private const string MessageFormat = "Calculated property allocates reference type.";
        private const string Description = "Calculated property allocates reference type.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: Description,
            helpLinkUri: HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleArrow, SyntaxKind.ArrowExpressionClause);
            context.RegisterSyntaxNodeAction(HandleGet, SyntaxKind.GetAccessorDeclaration);
        }

        private static void HandleArrow(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (!(context.Node.Parent is PropertyDeclarationSyntax))
            {
                return;
            }

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

        private static void HandleGet(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var getter = (AccessorDeclarationSyntax)context.Node;
            var property = (IPropertySymbol)((IMethodSymbol)context.ContainingSymbol).AssociatedSymbol;
            if (getter.Body == null || property.SetMethod != null)
            {
                return;
            }

            StatementSyntax single;
            if (!getter.Body.Statements.TryGetSingle(out single))
            {
                return;
            }

            var returnStatement = single as ReturnStatementSyntax;
            var objectCreation = returnStatement?.Expression as ObjectCreationExpressionSyntax;
            if (objectCreation == null)
            {
                return;
            }

            var type = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken).Type;
            if (!type.IsReferenceType)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, returnStatement.GetLocation()));
        }
    }
}