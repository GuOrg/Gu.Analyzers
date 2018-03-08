namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ArgumentListAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(GU0002NamedArgumentPositionMatches.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleArguments, SyntaxKind.ArgumentList);
        }

        private static void HandleArguments(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ArgumentListSyntax argumentList)
            {
                if (!argumentList.Arguments.TryFirst(x => x.NameColon != null, out _))
                {
                    return;
                }

                if (context.SemanticModel.GetSymbolSafe(argumentList.Parent, context.CancellationToken) is IMethodSymbol method)
                {
                    if (method.Parameters.Length != argumentList.Arguments.Count)
                    {
                        return;
                    }

                    for (var i = 0; i < argumentList.Arguments.Count; i++)
                    {
                        var argument = argumentList.Arguments[i];
                        var parameter = method.Parameters[i];
                        if (argument.NameColon?.Name is IdentifierNameSyntax nameColon &&
                            parameter.Name != nameColon.Identifier.ValueText)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(GU0002NamedArgumentPositionMatches.Descriptor, argumentList.GetLocation()));
                            return;
                        }
                    }
                }
            }
        }
    }
}
