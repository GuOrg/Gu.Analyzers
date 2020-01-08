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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0022UseGetOnly);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.SetAccessorDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is AccessorDeclarationSyntax { Body: null } setter &&
                context.ContainingSymbol is IMethodSymbol { DeclaredAccessibility: Accessibility.Private, AssociatedSymbol: IPropertySymbol { IsIndexer: false, IsOverride: false, IsAbstract: false, ExplicitInterfaceImplementations: { Length: 0 } } property })
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

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0022UseGetOnly, setter.GetLocation()));
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
