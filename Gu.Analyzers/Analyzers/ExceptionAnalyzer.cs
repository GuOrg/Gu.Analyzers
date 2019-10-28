namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ExceptionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0090DoNotThrowNotImplementedException);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => AnalyzeNode(c), SyntaxKind.ThrowStatement, SyntaxKind.ThrowExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            switch (context.Node)
            {
                case ThrowStatementSyntax throwStatementSyntax when throwStatementSyntax.Expression is { } expression:
                    FindException<NotImplementedException>(context, expression, Descriptors.GU0090DoNotThrowNotImplementedException);
                    break;
                case ThrowExpressionSyntax throwExpressionSyntax when throwExpressionSyntax.Expression is { } expression:
                    FindException<NotImplementedException>(context, expression, Descriptors.GU0090DoNotThrowNotImplementedException);
                    break;
                default:
                    return;
            }
        }

        private static void FindException<TException>(SyntaxNodeAnalysisContext context, ExpressionSyntax expressionSyntax, DiagnosticDescriptor diagnosticDescriptor)
            where TException : Exception
        {
            if (context.SemanticModel.TryGetType(expressionSyntax, context.CancellationToken, out var type) &&
                type.Name == typeof(TException).Name)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, expressionSyntax.GetLocation()));
            }
        }
    }
}
