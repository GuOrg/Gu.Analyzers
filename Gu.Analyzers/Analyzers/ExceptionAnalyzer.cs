namespace Gu.Analyzers.Analyzers
{
    using System;
    using System.Collections.Immutable;
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
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ThrowExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is ThrowStatementSyntax throwStatementSyntax)
            {
                var semanticModel = context.SemanticModel;
                var typeInfo = semanticModel.GetTypeInfo(throwStatementSyntax.Expression);

                if (typeInfo.Type.Name == typeof(NotImplementedException).Name)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GU0090DontThrowNotImplementedException.Descriptor, throwStatementSyntax.GetLocation()));
                }
            }
        }
    }
}
