namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class SimpleAssignmentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(GU0012NullCheckParameter.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is AssignmentExpressionSyntax assignment &&
                assignment.Right is IdentifierNameSyntax identifier &&
                context.ContainingSymbol is IMethodSymbol method &&
                method.DeclaredAccessibility.IsEither(Accessibility.Internal, Accessibility.Public) &&
                method.Parameters.TryFirst(x => x.Name == identifier.Identifier.ValueText, out var parameter) &&
                parameter.Type.IsReferenceType &&
                !parameter.HasExplicitDefaultValue)
            {
                context.ReportDiagnostic(Diagnostic.Create(GU0012NullCheckParameter.Descriptor, assignment.Right.GetLocation()));
            }
        }
    }
}
