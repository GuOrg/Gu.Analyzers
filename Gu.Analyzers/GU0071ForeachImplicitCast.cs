namespace Gu.Analyzers
{
    using System.Collections.Immutable;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0071ForeachImplicitCast : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0071ForeachImplicitCast);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ForEachStatement);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ForEachStatementSyntax forEachStatement &&
               !forEachStatement.Type.IsVar &&
               EnumeratorTypeMatchesTheVariableType(context, forEachStatement) == false)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0071ForeachImplicitCast, forEachStatement.Type.GetLocation()));
            }
        }

        private static bool? EnumeratorTypeMatchesTheVariableType(SyntaxNodeAnalysisContext context, ForEachStatementSyntax forEachStatement)
        {
            var enumerableType = context.SemanticModel.GetTypeInfoSafe(forEachStatement.Expression, context.CancellationToken);
            if (enumerableType.Type is IErrorTypeSymbol)
            {
                return null;
            }

            if (enumerableType.Type == KnownSymbol.IEnumerable &&
                enumerableType.ConvertedType == KnownSymbol.IEnumerable)
            {
                return true;
            }

            if (enumerableType.Type.IsAssignableTo(KnownSymbol.IEnumerable, context.Compilation))
            {
                if (enumerableType.ConvertedType is INamedTypeSymbol namedType &&
                    namedType.TypeArguments.TrySingle(out var enumerableTypeArg))
                {
                    var variableType = context.SemanticModel.GetTypeInfoSafe(forEachStatement.Type, context.CancellationToken).Type;
                    return variableType.Equals(enumerableTypeArg);
                }

                return enumerableType.ConvertedType != KnownSymbol.IEnumerable;
            }

            if (enumerableType.ConvertedType.TryFindFirstMethodRecursive("GetEnumerator", out var method) &&
                method.ReturnType is INamedTypeSymbol returnType &&
                returnType.TypeArguments.TrySingle(out var enumeratorTypeArg))
            {
                var variableType = context.SemanticModel.GetTypeInfoSafe(forEachStatement.Type, context.CancellationToken).Type;
                return variableType.Equals(enumeratorTypeArg);
            }

            return null;
        }
    }
}
