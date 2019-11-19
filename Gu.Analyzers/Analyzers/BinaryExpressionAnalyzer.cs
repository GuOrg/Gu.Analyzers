namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Reflection.Metadata;
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
                context.Node is BinaryExpressionSyntax and &&
                and.IsKind(SyntaxKind.LogicalAndExpression))
            {
                Handle(and.Left);
                Handle(and.Right);

                void Handle(ExpressionSyntax leftOrRight)
                {
                    if (Identifier(leftOrRight) is { } identifier &&
                        FindMergePattern(identifier, and) is { } mergeWith)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.GU0074PreferPattern,
                                leftOrRight.GetLocation(),
                                additionalLocations: new[] { mergeWith.GetLocation() }));
                    }
                    else if (CanConvert(leftOrRight))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.GU0074PreferPattern,
                                leftOrRight.GetLocation()));
                    }

                    static SyntaxNode? Identifier(ExpressionSyntax candidate)
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
                            IsPatternExpressionSyntax { Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax d } }
                                => d,
                            _ => null,
                        };
                    }
                }
            }
        }

        private static PatternSyntax? FindMergePattern(SyntaxNode identifier, ExpressionSyntax parent)
        {
            return parent switch
            {
                BinaryExpressionSyntax { Left: IsPatternExpressionSyntax left, OperatorToken: { ValueText: "&&" } } binary
                    when MergePattern(identifier, left) is { } mergePattern
                         => mergePattern,
                BinaryExpressionSyntax { OperatorToken: { ValueText: "&&" }, Right: IsPatternExpressionSyntax right } binary
                    when MergePattern(identifier, right) is { } mergePattern
                         => mergePattern,
                BinaryExpressionSyntax { Left: BinaryExpressionSyntax { OperatorToken: { ValueText: "&&" } } recursive }
                    => FindMergePattern(identifier, recursive),
                { Parent: WhenClauseSyntax { Parent: SwitchExpressionArmSyntax { Pattern: RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern } } }
                    when AreSame(identifier, designation)
                    => pattern,
                { Parent: WhenClauseSyntax { Parent: SwitchExpressionArmSyntax { Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern } } }
                    when AreSame(identifier, designation)
                        => pattern,
                { Parent: WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax { Pattern: RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern } } }
                    when AreSame(identifier, designation)
                    => pattern,
                { Parent: WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax { Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern } } }
                    when AreSame(identifier, designation)
                        => pattern,
                _ => null,
            };

            static PatternSyntax? MergePattern(SyntaxNode? identifier, IsPatternExpressionSyntax isPattern)
            {
                if (identifier is null ||
                    isPattern.Contains(identifier))
                {
                    return null;
                }

                return isPattern switch
                {
                    { Pattern: RecursivePatternSyntax { Designation: null } pattern }
                        when AreSame(identifier, isPattern.Expression)
                            => pattern,
                    { Pattern: RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern }
                        when AreSame(identifier, isPattern.Expression) || AreSame(identifier, designation)
                            => pattern,
                    { Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern }
                        when AreSame(identifier, isPattern.Expression) || AreSame(identifier, designation)
                            => pattern,
                    _ => null,
                };
            }

            static bool AreSame(SyntaxNode x, SyntaxNode y)
            {
                return (x, y) switch
                {
                    { x: IdentifierNameSyntax xn, y: IdentifierNameSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                    { x: IdentifierNameSyntax xn, y: SingleVariableDesignationSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                    { x: SingleVariableDesignationSyntax xn, y: IdentifierNameSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                    { x: SingleVariableDesignationSyntax xn, y: SingleVariableDesignationSyntax yn } => xn.Identifier.ValueText == yn.Identifier.ValueText,
                    _ => false,
                };
            }
        }

        private static bool CanConvert(ExpressionSyntax candidate)
        {
            switch (candidate)
            {
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }:
                case PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ } }:
                case BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, OperatorToken: { ValueText: "==" }, Right: LiteralExpressionSyntax _ }:
                case BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, OperatorToken: { ValueText: "!=" }, Right: LiteralExpressionSyntax { Token: { ValueText: "null" } } }:
                case IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax { Token: { ValueText: "null" } } } }:
                    return true;
                default:
                    return false;
            }
        }
    }
}
