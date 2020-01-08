namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0072AllTypesShouldBeInternal : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0072AllTypesShouldBeInternal);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is TypeDeclarationSyntax typeDeclaration &&
                context.ContainingSymbol is ITypeSymbol &&
                typeDeclaration.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.PublicKeyword), out var modifier))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0072AllTypesShouldBeInternal, modifier.GetLocation()));
            }
        }
    }
}
