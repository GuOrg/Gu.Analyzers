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
            context.RegisterSyntaxNodeAction(HandleField, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(HandleProperty, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(HandleUsing, SyntaxKind.UsingStatement);
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var field = (IFieldSymbol)context.ContainingSymbol;
            if (field.IsStatic)
            {
                return;
            }

            if (Disposable.IsAssignedWithInjectedOrCached(field, context.SemanticModel, context.CancellationToken))
            {
                CheckThatMemberIsNotDisposed(context);
            }
        }

        private static void HandleProperty(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var property = (IPropertySymbol)context.ContainingSymbol;
            if (property.IsStatic ||
                property.IsIndexer)
            {
                return;
            }

            if (Disposable.IsAssignedWithInjectedOrCached(property, context.SemanticModel, context.CancellationToken))
            {
                CheckThatMemberIsNotDisposed(context);
            }
        }

        private static void HandleUsing(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var usingStatement = (UsingStatementSyntax)context.Node;
            if (usingStatement.Expression is IdentifierNameSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, usingStatement.Expression.GetLocation()));
                return;
            }

            if (usingStatement.Expression is InvocationExpressionSyntax)
            {
                if (!Disposable.IsPotentialCreation(usingStatement.Expression, context.SemanticModel, context.CancellationToken))
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
                    if (!Disposable.IsPotentialCreation(value, context.SemanticModel, context.CancellationToken))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, value.GetLocation()));
                        return;
                    }
                }
            }
        }

        private static void CheckThatMemberIsNotDisposed(SyntaxNodeAnalysisContext context)
        {
            var containingType = context.ContainingSymbol.ContainingType;

            IMethodSymbol disposeMethod;
            if (!Disposable.IsAssignableTo(containingType) || !Disposable.TryGetDisposeMethod(containingType, true, out disposeMethod))
            {
                return;
            }

            foreach (var declaration in disposeMethod.Declarations(context.CancellationToken))
            {
                using (var pooled = IdentifierNameWalker.Create(declaration))
                {
                    foreach (var identifier in pooled.Item.IdentifierNames)
                    {
                        if (identifier.Identifier.ValueText != context.ContainingSymbol.Name)
                        {
                            continue;
                        }

                        var symbol = context.SemanticModel.GetSymbolSafe(identifier, context.CancellationToken);
                        if (ReferenceEquals(symbol, context.ContainingSymbol))
                        {
                            using (var invocations = InvocationWalker.Create(declaration))
                            {
                                foreach (var invocation in invocations.Item.Invocations)
                                {
                                    var method = context.SemanticModel.GetSymbolSafe(invocation, context.CancellationToken) as IMethodSymbol;
                                    if (method != KnownSymbol.IDisposable.Dispose)
                                    {
                                        continue;
                                    }

                                    ExpressionSyntax invokee;
                                    if (invocation.TryFindInvokee(out invokee))
                                    {
                                        if (ReferenceEquals(invokee, identifier) ||
                                            ReferenceEquals((invokee as MemberAccessExpressionSyntax)?.Name, identifier))
                                        {
                                            context.ReportDiagnostic(Diagnostic.Create(Descriptor, identifier.FirstAncestorOrSelf<StatementSyntax>()?.GetLocation() ?? identifier.GetLocation()));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}