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
                context.ContainingSymbol is IMethodSymbol method &&
                context.Node is ParameterSyntax { Identifier: { ValueText: { } valueText } } parameter &&
                method.TryFindParameter(valueText, out var parameterSymbol))
            {
                if (ShouldCheckNull())
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0012NullCheckParameter, parameter.Identifier.GetLocation()));
                }

                bool ShouldCheckNull()
                {
                    return method.DeclaredAccessibility.IsEither(Accessibility.Internal, Accessibility.Protected, Accessibility.Public) &&
                           parameterSymbol is { Type: { IsReferenceType: true }, HasExplicitDefaultValue: false } &&
                           parameterSymbol.RefKind != RefKind.Out &&
                           parameter.Parent is ParameterListSyntax { Parent: BaseMethodDeclarationSyntax methodDeclaration } &&
                           !NullCheck.IsChecked(parameterSymbol, methodDeclaration, context.SemanticModel, context.CancellationToken);
                }
            }
        }
    }
}
