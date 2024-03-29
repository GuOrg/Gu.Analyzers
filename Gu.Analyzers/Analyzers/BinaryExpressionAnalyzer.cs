﻿namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class BinaryExpressionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.GU0074PreferPattern,
        Descriptors.GU0076MergePattern,
        Descriptors.GU0077PreferIsNull);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => AnalyzeNode(c), SyntaxKind.LogicalAndExpression, SyntaxKind.EqualsExpression);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.Node is BinaryExpressionSyntax binaryExpression)
        {
            if (binaryExpression is { Left: { }, OperatorToken.ValueText: "==", Right: LiteralExpressionSyntax { Token.ValueText: "null" } })
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.GU0077PreferIsNull,
                        binaryExpression.GetLocation()));
            }

            if (binaryExpression is { Left: { } left, OperatorToken.ValueText: "&&", Right: { } right } and)
            {
                Handle(left);
                Handle(right);

                void Handle(ExpressionSyntax leftOrRight)
                {
                    if (Pattern.Identifier(leftOrRight, context.SemanticModel, context.CancellationToken) is { } identifier &&
                        FindMergePattern(identifier, and) is { } mergeWith)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.GU0076MergePattern,
                                leftOrRight.GetLocation(),
                                additionalLocations: new[] { mergeWith.GetLocation() }));
                    }
                    else if (CanConvert(leftOrRight, context.SemanticModel, context.CancellationToken))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.GU0074PreferPattern,
                                leftOrRight.GetLocation()));
                    }
                }
            }
        }
    }

    private static PatternSyntax? FindMergePattern(SyntaxNode identifier, BinaryExpressionSyntax parent)
    {
        return parent switch
        {
            { Left: IsPatternExpressionSyntax left, OperatorToken.ValueText: "&&" }
                when Pattern.MergePattern(identifier, left) is { } mergePattern
                => mergePattern,
            { OperatorToken.ValueText: "&&", Right: IsPatternExpressionSyntax right }
                when Pattern.MergePattern(identifier, right) is { } mergePattern
                => mergePattern,
            { Parent: WhenClauseSyntax { Parent: SwitchExpressionArmSyntax { Pattern: { } pattern } } }
                => Pattern.MergePattern(identifier, pattern),
            { Parent: WhenClauseSyntax { Parent: CasePatternSwitchLabelSyntax { Pattern: { } pattern } } }
                => Pattern.MergePattern(identifier, pattern),
            { Left: BinaryExpressionSyntax { OperatorToken.ValueText: "&&" } left }
                when !left.Contains(identifier)
                => FindMergePattern(identifier, left),
            { Parent: BinaryExpressionSyntax { Left: { } left, OperatorToken.ValueText: "&&" } gp }
                when left.Contains(identifier)
                => FindMergePattern(identifier, gp),
            _ => null,
        };
    }

    private static bool CanConvert(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        switch (candidate)
        {
            case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }:
            case PrefixUnaryExpressionSyntax { Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ } }:
            case BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, OperatorToken.ValueText: "==", Right: LiteralExpressionSyntax _ }:
            case BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, OperatorToken.ValueText: "==", Right: MemberAccessExpressionSyntax memberAccess }
                when semanticModel.GetTypeInfo(memberAccess, cancellationToken) is { Type.TypeKind: TypeKind.Enum }:
            case BinaryExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, OperatorToken.ValueText: "!=", Right: LiteralExpressionSyntax { Token.ValueText: "null" } }:
            case IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax _, Name: IdentifierNameSyntax _ }, Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax { Token.ValueText: "null" } } }:
                return true;
            default:
                return false;
        }
    }
}
