namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class StringLiteralExpressionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.GU0006UseNameof);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.StringLiteralExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is LiteralExpressionSyntax { Parent: ArgumentSyntax _ } literal &&
                SyntaxFacts.IsValidIdentifier(literal.Token.ValueText))
            {
                foreach (var symbol in context.SemanticModel.LookupSymbols(literal.SpanStart, name: literal.Token.ValueText))
                {
                    switch (symbol)
                    {
                        case IParameterSymbol _:
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0006UseNameof, literal.GetLocation()));
                            break;
                        case IFieldSymbol _:
                        case IEventSymbol _:
                        case IPropertySymbol _:
                        case IMethodSymbol _:
                            if (symbol.IsStatic)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0006UseNameof, literal.GetLocation()));
                            }
                            else
                            {
                                var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("member", symbol.Name) });
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0006UseNameof, literal.GetLocation(), properties));
                            }

                            break;
                        case ILocalSymbol local when IsVisible(literal, local, context.CancellationToken):
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0006UseNameof, literal.GetLocation()));
                            break;
                    }
                }
            }
        }

        private static bool IsVisible(LiteralExpressionSyntax literal, ILocalSymbol local, CancellationToken cancellationToken)
        {
            if (local.DeclaringSyntaxReferences.Length == 1 &&
                local.DeclaringSyntaxReferences[0].Span.Start < literal.SpanStart)
            {
                var declaration = local.DeclaringSyntaxReferences[0]
                                       .GetSyntax(cancellationToken);
                return !declaration.Contains(literal);
            }

            return false;
        }
    }
}
