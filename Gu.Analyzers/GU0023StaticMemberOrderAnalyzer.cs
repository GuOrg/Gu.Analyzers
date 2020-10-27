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
    internal class GU0023StaticMemberOrderAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0023StaticMemberOrder);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.FieldDeclaration, SyntaxKind.PropertyDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol.IsStatic)
            {
                switch (context.Node)
                {
                    case FieldDeclarationSyntax { Declaration: { Variables: { } variables } } field
                        when variables.Last() is { Initializer: { Value: { } value } } &&
                             IsInitializedWithUninitialized(field, value, context, out var other):
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0023StaticMemberOrder, value.GetLocation(), other.Symbol, context.ContainingSymbol));
                        break;
                    case PropertyDeclarationSyntax { Initializer: { Value: { } value } } property
                        when IsInitializedWithUninitialized(property, value, context, out var other):
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0023StaticMemberOrder, value.GetLocation(), other.Symbol, context.ContainingSymbol));
                        break;
                }
            }
        }

        private static bool IsInitializedWithUninitialized(MemberDeclarationSyntax member, ExpressionSyntax value, SyntaxNodeAnalysisContext context, out FieldOrProperty other)
        {
            using var walker = Walker.Borrow(value, context.SemanticModel, context.CancellationToken);
            foreach (var identifierName in walker.IdentifierNames)
            {
                if (!IsNameOf(identifierName) &&
                    context.SemanticModel.TryGetSymbol(identifierName, context.CancellationToken, out var symbol) &&
                    FieldOrProperty.TryCreate(symbol, out other) &&
                    other.IsStatic &&
                    Equals(other.ContainingType, context.ContainingSymbol.ContainingType) &&
                    symbol.TrySingleDeclaration(context.CancellationToken, out MemberDeclarationSyntax? otherDeclaration))
                {
                    if (otherDeclaration.SpanStart > context.Node.SpanStart &&
                        IsInitialized(otherDeclaration))
                    {
                        return true;
                    }

                    if (!IsInSamePart(member, otherDeclaration))
                    {
                        return true;
                    }
                }
            }

            return false;

            static bool IsInitialized(MemberDeclarationSyntax declaration)
            {
                return declaration switch
                {
                    PropertyDeclarationSyntax { Initializer: { } } => true,
                    FieldDeclarationSyntax { Declaration: { Variables: { } variables } } => variables.TryFirst(x => x.Initializer != null, out _),
                    _ => false,
                };
            }

            static bool IsInSamePart(MemberDeclarationSyntax x, MemberDeclarationSyntax y)
            {
                return x.TryFindSharedAncestorRecursive(y, out TypeDeclarationSyntax _);
            }
        }

        private static bool IsNameOf(IdentifierNameSyntax name)
        {
            return name.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { } invocation } } &&
                   invocation.TryGetMethodName(out var methodName) &&
                   methodName == "nameof";
        }

        private sealed class Walker : ExecutionWalker<Walker>
        {
            private readonly List<IdentifierNameSyntax> identifierNames = new List<IdentifierNameSyntax>();
            private readonly HashSet<ISymbol> symbols = new HashSet<ISymbol>(SymbolComparer.Default);

            private Walker()
            {
            }

            /// <summary>
            /// Gets the <see cref="IdentifierNameSyntax"/>s found in the scope.
            /// </summary>
            internal IReadOnlyList<IdentifierNameSyntax> IdentifierNames => this.identifierNames;

            public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                // don't walk lambda
            }

            public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                // don't walk lambda
            }

            /// <inheritdoc />
            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                this.identifierNames.Add(node);
                base.VisitIdentifierName(node);
            }

            /// <summary>
            /// Get a walker that has visited <paramref name="node"/>.
            /// </summary>
            /// <param name="node">The node.</param>
            /// <param name="semanticModel">The <see cref="SemanticModel"/>.</param>
            /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
            /// <returns>A walker that has visited <paramref name="node"/>.</returns>
            internal static Walker Borrow(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                return BorrowAndVisit(node, SearchScope.Type, semanticModel, cancellationToken, () => new Walker());
            }

            /// <inheritdoc />
            protected override void Clear()
            {
                this.identifierNames.Clear();
                this.symbols.Clear();
            }

            protected override bool TryGetTargetSymbol<TSource, TSymbol, TDeclaration>(TSource node, out Target<TSource, TSymbol, TDeclaration> target, string? caller = null, int line = 0)
            {
                return base.TryGetTargetSymbol(node, out target, caller, line) &&
                       this.symbols.Add(target.Symbol);
            }
        }
    }
}
