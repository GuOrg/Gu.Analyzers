namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0031DisposeMembers : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0031";
        private const string Title = "Dispose members.";
        private const string MessageFormat = "Dispose members.";
        private const string Description = "Dispose members.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.Correctness,
            DiagnosticSeverity.Warning,
            AnalyzerConstants.EnabledByDefault,
            Description,
            HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.VariableDeclarator);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!IsDisposableMember(context.ContainingSymbol))
            {
                return;
            }

            var containingType = context.ContainingSymbol.ContainingType;
            if (!Disposable.IsAssignableTo(containingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.Parent.Parent.GetLocation()));
                return;
            }

            foreach (var disposeMethod in containingType.GetMembers("Dispose").OfType<IMethodSymbol>())
            {
                MethodDeclarationSyntax declaration;
                if (disposeMethod.TryGetDeclaration(context.CancellationToken, out declaration))
                {
                    using (var walker = Disposable.DisposeWalker.Create(declaration.Body, context.SemanticModel, context.CancellationToken))
                    {
                        foreach (var disposeCall in walker.DisposeCalls)
                        {
                            var expressionSyntax = DisposedMember(disposeCall);
                            var disposedSymbol = context.SemanticModel.GetSymbolInfo(expressionSyntax, context.CancellationToken).Symbol;
                            if (ReferenceEquals(disposedSymbol, context.ContainingSymbol))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.Parent.Parent.GetLocation()));
        }

        private static ExpressionSyntax DisposedMember(InvocationExpressionSyntax disposeCall)
        {
            var memberAccess = disposeCall.Expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                return memberAccess.Expression;
            }

            if (disposeCall.Expression is MemberBindingExpressionSyntax)
            {
                var conditionalAccess = (ConditionalAccessExpressionSyntax)disposeCall.Parent;
                return conditionalAccess.Expression;
            }

            throw new ArgumentOutOfRangeException(nameof(disposeCall), disposeCall, "Could not find disposed member.");
        }

        private static bool IsDisposableMember(ISymbol symbol)
        {
            var field = symbol as IFieldSymbol;
            if (field != null)
            {
                return Disposable.IsAssignableTo(field.Type);
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                return Disposable.IsAssignableTo(property.Type);
            }

            return false;
        }
    }
}