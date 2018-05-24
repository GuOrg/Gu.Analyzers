namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0022UseGetOnly : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0022";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Use get-only.",
            messageFormat: "Use get-only.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Use get-only.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

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

            if (context.Node is AccessorDeclarationSyntax setter &&
                setter.Body == null &&
                context.ContainingSymbol is IMethodSymbol setMethod &&
                setMethod.DeclaredAccessibility == Accessibility.Private &&
                setMethod.AssociatedSymbol is IPropertySymbol property &&
                !property.IsIndexer)
            {
                using (var walker = MutationWalker.For(property, context.SemanticModel, context.CancellationToken))
                {
                    foreach (var value in walker.All())
                    {
                        if (MeansPropertyIsMutable(value))
                        {
                            return;
                        }
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, setter.GetLocation()));
            }
        }

        private static bool MeansPropertyIsMutable(SyntaxNode mutation)
        {
            switch (mutation)
            {
                case AssignmentExpressionSyntax assignment when !MemberPath.TrySingle(assignment.Left, out _):
                    return true;
                case PostfixUnaryExpressionSyntax unary when !MemberPath.TrySingle(unary.Operand, out _):
                    return true;
                case PrefixUnaryExpressionSyntax unary when !MemberPath.TrySingle(unary.Operand, out _):
                    return true;
            }

            if (mutation.TryFirstAncestorOrSelf<ConstructorDeclarationSyntax>(out _))
            {
                return mutation.TryFirstAncestor<AnonymousFunctionExpressionSyntax>(out _) ||
                       mutation.TryFirstAncestor<ObjectCreationExpressionSyntax>(out _);
            }

            return true;
        }
    }
}
