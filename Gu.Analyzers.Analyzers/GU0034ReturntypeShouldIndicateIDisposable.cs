namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0034ReturntypeShouldIndicateIDisposable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0034";
        private const string Title = "Return type should indicate that the value should be disposed.";
        private const string MessageFormat = "Return type should indicate that the value should be disposed.";
        private const string Description = "Return type should indicate that the value should be disposed.";
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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleReturnValue, SyntaxKind.ReturnStatement);
            context.RegisterSyntaxNodeAction(HandleArrow, SyntaxKind.ArrowExpressionClause);
        }

        private static void HandleReturnValue(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var symbol = context.ContainingSymbol;
            if (IsIgnored(symbol))
            {
                return;
            }

            if (IsDisposableReturnType(ReturnType(context)))
            {
                return;
            }

            var returnStatement = (ReturnStatementSyntax)context.Node;
            if (returnStatement.Expression == null)
            {
                return;
            }

            HandleReturnValue(context, returnStatement.Expression);
        }

        private static void HandleArrow(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var symbol = context.ContainingSymbol;
            if (IsIgnored(symbol))
            {
                return;
            }

            if (IsDisposableReturnType(ReturnType(context)))
            {
                return;
            }

            var arrowClause = (ArrowExpressionClauseSyntax)context.Node;
            if (arrowClause.Expression == null)
            {
                return;
            }

            HandleReturnValue(context, arrowClause.Expression);
        }

        private static void HandleReturnValue(SyntaxNodeAnalysisContext context, ExpressionSyntax returnValue)
        {
            if (Disposable.IsPotentialCreation(returnValue, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, returnValue.GetLocation()));
            }
        }

        private static bool IsDisposableReturnType(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (Disposable.IsAssignableTo(type))
            {
                return true;
            }

            if (type == KnownSymbol.Task)
            {
                var namedType = type as INamedTypeSymbol;
                return namedType?.IsGenericType == true && Disposable.IsAssignableTo(namedType.TypeArguments[0]);
            }

            if (type == KnownSymbol.Func)
            {
                var namedType = type as INamedTypeSymbol;
                return namedType?.IsGenericType == true && Disposable.IsAssignableTo(namedType.TypeArguments[namedType.TypeArguments.Length - 1]);
            }

            return false;
        }

        private static bool IsIgnored(ISymbol symbol)
        {
            var method = symbol as IMethodSymbol;
            if (method != null)
            {
                return method == KnownSymbol.IEnumerable.GetEnumerator;
            }

            return false;
        }

        private static ITypeSymbol ReturnType(SyntaxNodeAnalysisContext context)
        {
            var anonymousFunction = context.Node.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>();
            if (anonymousFunction != null)
            {
                var method = context.SemanticModel.GetSymbolSafe(anonymousFunction, context.CancellationToken) as IMethodSymbol;
                return method?.ReturnType;
            }

            return (context.ContainingSymbol as IMethodSymbol)?.ReturnType ??
                   (context.ContainingSymbol as IFieldSymbol)?.Type ??
                   (context.ContainingSymbol as IPropertySymbol)?.Type;
        }
    }
}