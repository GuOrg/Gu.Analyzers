namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Pattern
    {
        internal static IdentifierNameSyntax? Identifier(ExpressionSyntax candidate)
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
                IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax _ } }
                => e,
                _ => null,
            };
        }
    }
}
