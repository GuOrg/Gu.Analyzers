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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0020SortProperties);

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
                propertyDeclaration.Parent is TypeDeclarationSyntax { Members: { } members })
            {
                var index = members.IndexOf(propertyDeclaration);
                if (members.TryElementAt(index + 1, out var after) &&
                    (after is PropertyDeclarationSyntax || after is IndexerDeclarationSyntax))
                {
                    if (MemberDeclarationComparer.Compare(propertyDeclaration, after) > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0020SortProperties, context.Node.GetLocation()));
                    }
                }
            }
        }
    }
}
