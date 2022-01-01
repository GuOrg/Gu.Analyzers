namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class PropertyDeclarationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.GU0008AvoidRelayProperties,
        Descriptors.GU0021CalculatedPropertyAllocates);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.PropertyDeclaration);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (context.IsExcludedFromAnalysis())
        {
            return;
        }

        if (context.Node is PropertyDeclarationSyntax propertyDeclaration &&
            context.ContainingSymbol is IPropertySymbol { GetMethod: { } } property &&
            ReturnValueWalker.TrySingle(propertyDeclaration, out var returnValue))
        {
            if (property is { Type: { IsReferenceType: true }, SetMethod: null } &&
                returnValue is ObjectCreationExpressionSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0021CalculatedPropertyAllocates, returnValue.GetLocation()));
            }
            else if (returnValue is MemberAccessExpressionSyntax memberAccess &&
                     IsRelayReturn(memberAccess, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0008AvoidRelayProperties, memberAccess.GetLocation()));
            }
        }
    }

    private static bool IsRelayReturn(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return memberAccess switch
        {
            { Expression: IdentifierNameSyntax expression, Name: IdentifierNameSyntax _ } => IsAssignedWithInjected(expression),
            { Expression: MemberAccessExpressionSyntax expression, Name: IdentifierNameSyntax _ } => IsAssignedWithInjected(expression),
            _ => false,
        };

        bool IsAssignedWithInjected(ExpressionSyntax candidate)
        {
            return semanticModel.TryGetSymbol(candidate, cancellationToken, out ISymbol? member) &&
                   FieldOrProperty.TryCreate(member, out FieldOrProperty fieldOrProperty) &&
                   memberAccess.TryFirstAncestor<TypeDeclarationSyntax>(out var typeDeclaration) &&
                   IsInjected(fieldOrProperty, typeDeclaration, semanticModel, cancellationToken);
        }
    }

    private static bool IsInjected(FieldOrProperty member, TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        using (var walker = AssignmentExecutionWalker.For(member.Symbol, typeDeclaration, SearchScope.Instance, semanticModel, cancellationToken))
        {
            foreach (var assignment in walker.Assignments)
            {
                if (assignment.TryFirstAncestorOrSelf<ConstructorDeclarationSyntax>(out _) &&
                    semanticModel.GetSymbolSafe(assignment.Right, cancellationToken) is IParameterSymbol)
                {
                    return true;
                }
            }
        }

        return false;
    }
}