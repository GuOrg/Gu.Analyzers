namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MethodGroupAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0016PreferLambda);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.Argument, SyntaxKind.AddAssignmentExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            switch (context.Node)
            {
                case ArgumentSyntax argument when IsMethodGroup(argument.Expression, context):
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0016PreferLambda, argument.Expression.GetLocation()));
                    break;
                case AssignmentExpressionSyntax assignment when
                     assignment.IsKind(SyntaxKind.AddAssignmentExpression) &&
                     IsMethodGroup(assignment.Right, context):
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0016PreferLambda, assignment.Right.GetLocation()));
                    break;
                }
            }
        }

        private static bool IsMethodGroup(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
        {
            return expression is IdentifierNameSyntax identifierName &&
                   context.SemanticModel.TryGetSymbol(identifierName, context.CancellationToken, out IMethodSymbol? method) &&
                   method.IsStatic;
        }
    }
}
