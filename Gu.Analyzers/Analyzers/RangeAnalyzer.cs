namespace Gu.Analyzers;

using System.Collections.Immutable;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class RangeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.GU0026RangeAllocation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.BracketedArgumentList);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.Node is BracketedArgumentListSyntax { } range &&
            Allocates(range))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0026RangeAllocation, range.GetLocation()));
        }

        bool Allocates(BracketedArgumentListSyntax candidate)
        {
            if (candidate.Parent is ElementAccessExpressionSyntax { Expression: { } expression } &&
                context.SemanticModel.GetType(expression, context.CancellationToken) is IArrayTypeSymbol or INamedTypeSymbol { MetadataName: "string" })
            {
                return true;
            }

            return false;
        }
    }
}
