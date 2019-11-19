namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class BinaryExpressionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0074PreferPattern);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => AnalyzeNode(c), SyntaxKind.LogicalAndExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is BinaryExpressionSyntax binaryExpression &&
                binaryExpression.IsKind(SyntaxKind.LogicalAndExpression))
            {
                if (Expression(binaryExpression.Left) is { })
                {
                    if (binaryExpression.TryFirstAncestor(out WhenClauseSyntax? whenClause))
                    {
                        if (WhenAnalyzer.Pattern(BranchExpression(binaryExpression.Left), whenClause) is { } leftPattern)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.GU0074PreferPattern,
                                    binaryExpression.Left.GetLocation(),
                                    additionalLocations: new[] { leftPattern.GetLocation() }));
                        }

                        if (WhenAnalyzer.Pattern(BranchExpression(binaryExpression.Right), whenClause) is { } rightPattern)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.GU0074PreferPattern,
                                    binaryExpression.Right.GetLocation(),
                                    additionalLocations: new[] { rightPattern.GetLocation() }));
                        }

                        ExpressionSyntax BranchExpression(ExpressionSyntax e)
                        {
                            return e switch
                            {
                                MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifierName }
                                    => identifierName,
                                _ => e,
                            };
                        }
                    }

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.GU0074PreferPattern,
                            binaryExpression.Left.GetLocation()));
                }
                else if (binaryExpression.Left is IsPatternExpressionSyntax { Expression: IdentifierNameSyntax _ } isPattern &&
                         Expression(binaryExpression.Right) is { } expression)
                {
                    switch (isPattern)
                    {
                        case { Pattern: RecursivePatternSyntax { Designation: null } }
                            when AreSame(expression, isPattern.Expression):
                        case { Pattern: RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax rd } }
                            when AreSame(expression, rd):
                        case { Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax dd } }
                            when AreSame(expression, dd):
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.GU0074PreferPattern,
                                    binaryExpression.Right.GetLocation(),
                                    additionalLocations: new[] { isPattern.GetLocation() }));
                            break;
                    }
                }

                static bool AreSame(ExpressionSyntax x, SyntaxNode y)
                {
                    return (x, y) switch
                    {
                        { x: IdentifierNameSyntax xn, y: IdentifierNameSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                        { x: IdentifierNameSyntax xn, y: SingleVariableDesignationSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                        _ => false,
                    };
                }

                static ExpressionSyntax? Expression(ExpressionSyntax candidate)
                {
                    return candidate switch
                    {
                        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }
                            => e,
                        PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ } }
                            => e,
                        BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, OperatorToken: { ValueText: "==" }, Right: LiteralExpressionSyntax _ }
                            => e,
                        BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, OperatorToken: { ValueText: "!=" }, Right: LiteralExpressionSyntax { Token: { ValueText: "null" } } }
                            => e,
                        IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, Pattern: ConstantPatternSyntax _ }
                            => e,
                        _ => null,
                    };
                }
            }
        }
    }
}
