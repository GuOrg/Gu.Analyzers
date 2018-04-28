namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ParameterAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GU0012NullCheckParameter.Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.Parameter);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ParameterSyntax parameterSyntax &&
                context.ContainingSymbol is IMethodSymbol method &&
                method.DeclaredAccessibility.IsEither(Accessibility.Internal, Accessibility.Protected, Accessibility.Public) &&
                method.TryFindParameter(parameterSyntax.Identifier.ValueText, out var parameter) &&
                parameter.Type.IsReferenceType &&
                !parameter.HasExplicitDefaultValue &&
                !NullCheck.IsChecked(parameter, parameterSyntax.FirstAncestor<BaseMethodDeclarationSyntax>(), context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(GU0012NullCheckParameter.Descriptor, parameterSyntax.Identifier.GetLocation()));
            }
        }
    }
}
