namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.AnalyzerExtensions.StyleCopComparers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0020SortProperties : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "GU0020";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Sort properties.",
            messageFormat: "Move property.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Sort properties by StyleCop rules then by mutability.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.IndexerDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is BasePropertyDeclarationSyntax propertyDeclaration &&
                propertyDeclaration.TryFirstAncestor<TypeDeclarationSyntax>(out var typeDeclaration))
            {
                var index = typeDeclaration.Members.IndexOf(propertyDeclaration);
                if (typeDeclaration.Members.TryElementAt(index + 1, out var after) &&
                    (after is PropertyDeclarationSyntax || after is IndexerDeclarationSyntax))
                {
                    if (MemberDeclarationComparer.Compare(propertyDeclaration, after) > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                    }
                }
            }
        }
    }
}
