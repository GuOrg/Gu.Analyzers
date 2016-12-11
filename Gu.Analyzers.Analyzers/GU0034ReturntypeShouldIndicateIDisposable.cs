namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0034ReturntypeShouldIndicateIDisposable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0034";
        private const string Title = "Returntype should indicate that the value should be disposed.";
        private const string MessageFormat = "Returntype should indicate that the value should be disposed.";
        private const string Description = "Returntype should indicate that the value should be disposed.";
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
            context.RegisterSyntaxNodeAction(HandleReturn, SyntaxKind.ReturnStatement);
            context.RegisterSyntaxNodeAction(HandleArrow, SyntaxKind.ArrowExpressionClause);
        }

        private static void HandleReturn(SyntaxNodeAnalysisContext context)
        {
            var symbol = context.ContainingSymbol;
            if (Disposable.IsAssignableTo(MemberType(symbol)))
            {
                return;
            }

            var returnStatement = (ReturnStatementSyntax)context.Node;
            if (returnStatement.Expression == null)
            {
                return;
            }

            if (Disposable.IsCreation(returnStatement.Expression, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, returnStatement.Expression.GetLocation()));
            }
        }

        private static void HandleArrow(SyntaxNodeAnalysisContext context)
        {
            var symbol = context.ContainingSymbol;
            if (Disposable.IsAssignableTo(MemberType(symbol)))
            {
                return;
            }

            var arrowClause = (ArrowExpressionClauseSyntax)context.Node;
            if (arrowClause.Expression == null)
            {
                return;
            }

            if (Disposable.IsCreation(arrowClause.Expression, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, arrowClause.Expression.GetLocation()));
            }
        }

        private static ITypeSymbol MemberType(ISymbol member) =>
            (member as IMethodSymbol)?.ReturnType ??
            (member as IFieldSymbol)?.Type ??
            (member as IPropertySymbol)?.Type;
    }
}