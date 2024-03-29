﻿namespace Gu.Analyzers;

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Pattern
{
    internal static IdentifierNameSyntax? Identifier(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return candidate switch
        {
            MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }
                => e,
            PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, OperatorToken.ValueText: "!" }
                => e,
            BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, OperatorToken.ValueText: "==", Right: LiteralExpressionSyntax _ }
                => e,
            BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, OperatorToken.ValueText: "==", Right: MemberAccessExpressionSyntax memberAccess }
                when semanticModel.GetTypeInfo(memberAccess, cancellationToken) is { Type.TypeKind: TypeKind.Enum }
                => e,
            BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, OperatorToken.ValueText: "!=", Right: LiteralExpressionSyntax { Token.ValueText: "null" } }
                => e,
            IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, Pattern: ConstantPatternSyntax _ }
                => e,
            IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax _ } }
                => e,
            IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax e, Name: IdentifierNameSyntax _ }, Pattern: RecursivePatternSyntax { PositionalPatternClause: null, PropertyPatternClause.Subpatterns.Count: 0 } }
                => e,
            _ => null,
        };
    }

    internal static PatternSyntax? MergePattern(SyntaxNode identifier, IsPatternExpressionSyntax isPattern)
    {
        return isPattern switch
        {
            { Pattern: RecursivePatternSyntax { Designation: null } pattern }
                when AreSame(identifier, isPattern.Expression)
                => pattern,
            { Pattern: RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern }
                when AreSame(identifier, designation)
                => pattern,
            { Pattern: RecursivePatternSyntax pattern }
                when AreSame(identifier, isPattern.Expression)
                => pattern,
            { Pattern: RecursivePatternSyntax pattern }
                => MergePattern(identifier, pattern),
            { Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax designation } pattern }
                when AreSame(identifier, isPattern.Expression) || AreSame(identifier, designation)
                => pattern,
            _ => null,
        };
    }

    internal static PatternSyntax? MergePattern(SyntaxNode identifier, PatternSyntax pattern)
    {
        return pattern switch
        {
            RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax designation }
                when AreSame(identifier, designation)
                => pattern,
            RecursivePatternSyntax { PropertyPatternClause: { } propertyPatterns }
                => SubPattern(propertyPatterns.Subpatterns),
            DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax designation }
                when AreSame(identifier, designation)
                => pattern,
            _ => null,
        };

        PatternSyntax? SubPattern(SeparatedSyntaxList<SubpatternSyntax> subPatterns)
        {
            foreach (var subPattern in subPatterns)
            {
                if (MergePattern(identifier, subPattern.Pattern) is { } match)
                {
                    return match;
                }
            }

            return null;
        }
    }

    private static bool AreSame(SyntaxNode x, SyntaxNode y)
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
