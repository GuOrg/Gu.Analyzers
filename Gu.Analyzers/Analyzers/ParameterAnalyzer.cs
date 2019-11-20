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
            Descriptors.GU0012NullCheckParameter,
            Descriptors.GU0075PreferReturnNullable);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.Parameter);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                 context.Node is ParameterSyntax { Identifier: { ValueText: { } valueText }, Parent: ParameterListSyntax { Parameters: { } parameters } parameterList } parameter)
            {
                if (ShouldCheckNull())
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0012NullCheckParameter, parameter.Identifier.GetLocation()));
                }

                if (parameter.Modifiers.Any(SyntaxKind.OutKeyword) &&
                    parameters.TrySingle(x => x.Modifiers.Count > 0, out _) &&
                    context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken) is { Type: { IsValueType: false }, ContainingSymbol: IMethodSymbol { ReturnType: { SpecialType: SpecialType.System_Boolean } } })
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0075PreferReturnNullable, parameter.GetLocation()));
                }

                bool ShouldCheckNull()
                {
                    return context.ContainingSymbol is IMethodSymbol method &&
                           method.TryFindParameter(valueText, out var parameterSymbol) &&
                           method.DeclaredAccessibility.IsEither(Accessibility.Internal, Accessibility.Protected, Accessibility.Public) &&
                           parameterSymbol is { Type: { IsReferenceType: true }, HasExplicitDefaultValue: false } &&
                           parameterSymbol.RefKind != RefKind.Out &&
                           parameter.Parent is ParameterListSyntax { Parent: BaseMethodDeclarationSyntax methodDeclaration } &&
                           !NullCheck.IsChecked(parameterSymbol, methodDeclaration, context.SemanticModel, context.CancellationToken);
                }
            }
        }
    }
}
