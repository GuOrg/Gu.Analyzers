namespace Gu.Analyzers
{
    using System.Collections.Immutable;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0036DontDisposeInjected : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0036";
        private const string Title = "Don't dispose injected.";
        private const string MessageFormat = "Don't dispose injected.";
        private const string Description = "Don't dispose disposables you do not own.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                                                                      id: DiagnosticId,
                                                                      title: Title,
                                                                      messageFormat: MessageFormat,
                                                                      category: AnalyzerCategory.Correctness,
                                                                      defaultSeverity: DiagnosticSeverity.Warning,
                                                                      isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
                                                                      description: Description,
                                                                      helpLinkUri: HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleUsing, SyntaxKind.UsingStatement);
            context.RegisterSyntaxNodeAction(HandleInvocation, SyntaxKind.InvocationExpression);
        }

        private static void HandleUsing(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var usingStatement = (UsingStatementSyntax)context.Node;
            if (usingStatement.Expression is InvocationExpressionSyntax ||
                usingStatement.Expression is IdentifierNameSyntax)
            {
                if (Disposable.IsPotentiallyCachedOrInjected(usingStatement.Expression, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, usingStatement.Expression.GetLocation()));
                    return;
                }
            }

            if (usingStatement.Declaration != null)
            {
                foreach (var variableDeclarator in usingStatement.Declaration.Variables)
                {
                    if (variableDeclarator.Initializer == null)
                    {
                        continue;
                    }

                    var value = variableDeclarator.Initializer.Value;
                    if (Disposable.IsPotentiallyCachedOrInjected(value, context.SemanticModel, context.CancellationToken))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, value.GetLocation()));
                        return;
                    }
                }
            }
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var invocation = context.Node as InvocationExpressionSyntax;
            if (invocation == null ||
                invocation?.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() != null)
            {
                return;
            }

            var call = context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken) as IMethodSymbol;
            if (call != KnownSymbol.IDisposable.Dispose)
            {
                return;
            }

            var statement = invocation.FirstAncestor<ExpressionStatementSyntax>();
            if (Disposable.IsPotentiallyCachedOrInjected(statement, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, invocation.FirstAncestorOrSelf<StatementSyntax>()?.GetLocation() ?? invocation.GetLocation()));
            }
        }
    }
}