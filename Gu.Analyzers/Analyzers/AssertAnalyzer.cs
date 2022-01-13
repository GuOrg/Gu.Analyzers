namespace Gu.Analyzers;

using System;
using System.Collections.Immutable;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class AssertAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.GU0084AssertExceptionMessage);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.InvocationExpression);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.Node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier: { ValueText: "Assert" } }, Name: { } name } } invocation &&
            name.Identifier.ValueText.StartsWith("Throws", StringComparison.InvariantCulture))
        {
            if (invocation.Parent is ExpressionStatementSyntax ||
                invocation.Parent is AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier: { ValueText: "_" } } })
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0084AssertExceptionMessage, invocation.GetLocation()));
            }
        }
    }
}
