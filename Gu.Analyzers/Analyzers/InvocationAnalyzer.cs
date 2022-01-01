namespace Gu.Analyzers;

using System.Collections.Immutable;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class InvocationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.GU0019LinqOrDefaultStructs);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.InvocationExpression);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.Node is InvocationExpressionSyntax invocation &&
            IsLinqOrDefaultStruct(invocation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0019LinqOrDefaultStructs, invocation.GetLocation()));
        }

        bool IsLinqOrDefaultStruct(InvocationExpressionSyntax candidate)
        {
            if (candidate.IsSymbol(KnownSymbols.Enumerable.FirstOrDefault, context.SemanticModel, context.CancellationToken) ||
                candidate.IsSymbol(KnownSymbols.Enumerable.LastOrDefault, context.SemanticModel, context.CancellationToken) ||
                candidate.IsSymbol(KnownSymbols.Enumerable.SingleOrDefault, context.SemanticModel, context.CancellationToken))
            {
                if (candidate.Expression is MemberAccessExpressionSyntax { Expression: { } expression } &&
                    context.SemanticModel.GetTypeInfoSafe(expression, context.CancellationToken) is { ConvertedType: { } enumerableType } &&
                    enumerableType.TryFindFirstMethodRecursive("GetEnumerator", out var method) &&
                    method.ReturnType is INamedTypeSymbol { IsGenericType: true, TypeArguments: { Length: 1 } typeArguments })
                {
                    return typeArguments[0].IsValueType && typeArguments[0].MetadataName != "Nullable`1";
                }
            }

            return false;
        }
    }
}
