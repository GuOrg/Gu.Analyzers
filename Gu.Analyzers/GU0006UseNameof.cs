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
    internal class GU0006UseNameof : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "GU0006";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Use nameof.",
            messageFormat: "Use nameof.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Use nameof.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.StringLiteralExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is LiteralExpressionSyntax literal &&
                literal.Parent is ArgumentSyntax &&
                SyntaxFacts.IsValidIdentifier(literal.Token.ValueText))
            {
                foreach (var symbol in context.SemanticModel.LookupSymbols(literal.SpanStart, name: literal.Token.ValueText))
                {
                    switch (symbol)
                    {
                        case IParameterSymbol _:
                            context.ReportDiagnostic(Diagnostic.Create(Descriptor, literal.GetLocation()));
                            break;
                        case IFieldSymbol _:
                        case IEventSymbol _:
                        case IPropertySymbol _:
                        case IMethodSymbol _:
                            if (symbol.IsStatic)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, literal.GetLocation()));
                            }
                            else
                            {
                                var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("member", symbol.Name) });
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, literal.GetLocation(), properties));
                            }

                            break;
                        case ILocalSymbol local when IsVisible(literal, local, context.CancellationToken):
                            context.ReportDiagnostic(Diagnostic.Create(Descriptor, literal.GetLocation()));
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
