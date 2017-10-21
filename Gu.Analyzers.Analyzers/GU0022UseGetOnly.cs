namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0022UseGetOnly : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0022";
        private const string Title = "Use get-only.";
        private const string MessageFormat = "Use get-only.";
        private const string Description = "Use get-only.";
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
            context.RegisterSyntaxNodeAction(HandleProperty, SyntaxKind.SetAccessorDeclaration);
        }

        private static void HandleProperty(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            AccessorDeclarationSyntax setter = (AccessorDeclarationSyntax)context.Node;
            if (setter.Body != null)
            {
                return;
            }

            var propertySymbol = context.ContainingProperty();
            if (propertySymbol.SetMethod?.DeclaredAccessibility != Accessibility.Private ||
                propertySymbol.IsIndexer)
            {
                return;
            }

            using (var walker = AssignedValueWalker.Borrow(propertySymbol, context.SemanticModel, context.CancellationToken))
            {
                foreach (var value in walker)
                {
                    if (MeansPropertyIsMutable(value))
                    {
                        return;
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, setter.GetLocation()));
        }

        private static bool MeansPropertyIsMutable(ExpressionSyntax assignedValue)
        {
            if (assignedValue.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() != null)
            {
                if (assignedValue.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() != null)
                {
                    return true;
                }

                return !IsAssigningMember(assignedValue);
            }

            var propertyDeclaration = assignedValue.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (propertyDeclaration != null)
            {
                return assignedValue.Parent != propertyDeclaration.Initializer;
            }

            return true;
        }

        private static bool IsAssigningMember(ExpressionSyntax assignedValue)
        {
            if (assignedValue.Parent is AssignmentExpressionSyntax assignment)
            {
                if (assignment.Left is IdentifierNameSyntax ||
                    (assignment.Left as MemberAccessExpressionSyntax)?.Expression is ThisExpressionSyntax ||
                    (assignment.Left as MemberAccessExpressionSyntax)?.Expression is BaseExpressionSyntax)
                {
                    return true;
                }
            }

            var operand = (assignedValue.Parent as PostfixUnaryExpressionSyntax)?.Operand ??
                                   (assignedValue.Parent as PrefixUnaryExpressionSyntax)?.Operand;
            if (operand != null)
            {
                if (operand is IdentifierNameSyntax ||
                    (operand as MemberAccessExpressionSyntax)?.Expression is ThisExpressionSyntax ||
                    (operand as MemberAccessExpressionSyntax)?.Expression is BaseExpressionSyntax)
                {
                    return true;
                }
            }

            return false;
        }
    }
}