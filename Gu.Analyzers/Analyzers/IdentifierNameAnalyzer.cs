namespace Gu.Analyzers
{
    using System.Collections.Immutable;

    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class IdentifierNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0017DoNotUseDiscarded);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.IdentifierName);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is IdentifierNameSyntax name &&
                name.Identifier.ValueText == "_" &&
                IsUsed(name))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0017DoNotUseDiscarded, name.Identifier.GetLocation()));
            }

            static bool IsUsed(IdentifierNameSyntax candidate)
            {
                return candidate.Parent switch
                {
                    ArgumentSyntax arg when arg.RefOrOutKeyword.IsKind(SyntaxKind.None) => true,
                    ExpressionSyntax e when !e.IsKind(SyntaxKind.SimpleAssignmentExpression) => true,
                    _ => false,
                };
            }
        }
    }
}
