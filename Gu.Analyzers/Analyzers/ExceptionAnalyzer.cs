namespace Gu.Analyzers.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ExceptionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(GU0090DontThrowNotImplementedException.Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ThrowStatement, SyntaxKind.ThrowExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            ExpressionSyntax expressionSyntax = null;

            if (context.Node is ThrowStatementSyntax throwStatementSyntax)
            {
                expressionSyntax = throwStatementSyntax.Expression;
            }
            else if (context.Node is ThrowExpressionSyntax throwExpressionSyntax)
            {
                expressionSyntax = throwExpressionSyntax.Expression;
            }

            this.FindException<NotImplementedException>(context, expressionSyntax, GU0090DontThrowNotImplementedException.Descriptor);
        }

        private void FindException<TException>(SyntaxNodeAnalysisContext context, ExpressionSyntax expressionSyntax, DiagnosticDescriptor diagnosticDescriptor) where TException : Exception
        {
            var typeInfo = context.SemanticModel.GetTypeInfo(expressionSyntax);

            if (typeInfo.Type.Name == typeof(TException).Name)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, expressionSyntax.GetLocation()));
            }
        }
    }
}
