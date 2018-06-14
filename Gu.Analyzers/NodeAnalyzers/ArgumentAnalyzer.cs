namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ArgumentAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GU0016PreferLambda.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.Argument);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ArgumentSyntax argument &&
                IsMethodGroup(argument, context))
            {
                context.ReportDiagnostic(Diagnostic.Create(GU0016PreferLambda.Descriptor, argument.GetLocation()));
            }
        }

        private static bool IsMethodGroup(ArgumentSyntax argument, SyntaxNodeAnalysisContext context)
        {
            return argument.Expression is IdentifierNameSyntax identifierName &&
                   context.SemanticModel.TryGetSymbol(identifierName, context.CancellationToken, out IMethodSymbol method) &&
                   method.IsStatic;
        }
    }
}
