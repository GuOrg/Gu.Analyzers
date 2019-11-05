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
            Descriptors.GU0012NullCheckParameter);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.Parameter);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is ParameterSyntax { Identifier: { ValueText: { } valueText }, Parent: ParameterListSyntax { Parent: BaseMethodDeclarationSyntax methodDeclaration } } parameterSyntax &&
                context.ContainingSymbol is IMethodSymbol method &&
                method.DeclaredAccessibility.IsEither(Accessibility.Internal, Accessibility.Protected, Accessibility.Public) &&
                method.TryFindParameter(valueText, out var parameter) &&
                parameter is { Type: { IsReferenceType: true }, HasExplicitDefaultValue: false } &&
                parameter.RefKind != RefKind.Out &&
                !NullCheck.IsChecked(parameter, methodDeclaration, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0012NullCheckParameter, parameterSyntax.Identifier.GetLocation()));
            }
        }
    }
}
