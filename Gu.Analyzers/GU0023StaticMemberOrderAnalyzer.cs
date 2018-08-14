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
        public const string DiagnosticId = "GU0023";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Static members that initialize with other static members depend on document order.",
            messageFormat: "Member '{0}' must be declared before '{1}'",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Static members that initialize with other static members depend on document order.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

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
                if (context.Node is FieldDeclarationSyntax fieldDeclaration &&
                    fieldDeclaration.Declaration is VariableDeclarationSyntax declaration &&
                    declaration.Variables.TryFirst(x => x.Initializer != null, out var variable) &&
                    variable.Initializer.Value is ExpressionSyntax fieldValue &&
                    IsInitializedWithUninitialized(fieldValue, context, out var other))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, fieldValue.GetLocation(), other.Symbol, context.ContainingSymbol));
                }

                if (context.Node is PropertyDeclarationSyntax propertyDeclaration &&
                    propertyDeclaration.Initializer?.Value is ExpressionSyntax propertyValue &&
                    IsInitializedWithUninitialized(propertyValue, context, out other))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, propertyValue.GetLocation(), other.Symbol, context.ContainingSymbol));
                }
            }
        }

        private static bool IsInitializedWithUninitialized(ExpressionSyntax value, SyntaxNodeAnalysisContext context, out FieldOrProperty other)
        {
            using (var walker = Walker.Borrow(value, context.SemanticModel, context.CancellationToken))
            {
                foreach (var identifierName in walker.IdentifierNames)
                {
                    if (!IsNameOf(identifierName) &&
                        context.SemanticModel.TryGetSymbol(identifierName, context.CancellationToken, out ISymbol symbol) &&
                        FieldOrProperty.TryCreate(symbol, out other) &&
                        other.IsStatic &&
                        other.ContainingType == context.ContainingSymbol.ContainingType &&
                        symbol.TrySingleDeclaration(context.CancellationToken, out MemberDeclarationSyntax otherDeclaration) &&
                        otherDeclaration.SpanStart > context.Node.SpanStart &&
                        IsInitialized(otherDeclaration))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsNameOf(IdentifierNameSyntax name)
        {
            return name.Parent is ArgumentSyntax arg &&
                   arg.Parent is ArgumentListSyntax argList &&
                   argList.Parent is InvocationExpressionSyntax invocation &&
                   invocation.TryGetMethodName(out var methodName) &&
                   methodName == "nameof";
        }

        private static bool IsInitialized(MemberDeclarationSyntax declaration)
        {
            switch (declaration)
            {
                case PropertyDeclarationSyntax propertyDeclaration:
                    return propertyDeclaration.Initializer != null;
                case FieldDeclarationSyntax fieldDeclaration:
                    return fieldDeclaration.Declaration is VariableDeclarationSyntax variableDeclaration &&
                           variableDeclaration.Variables.TryFirst(x => x.Initializer != null, out _);
                default:
                    return false;
            }
        }

        private sealed class Walker : ExecutionWalker<Walker>
        {
            private readonly List<IdentifierNameSyntax> identifierNames = new List<IdentifierNameSyntax>();

            private Walker()
            {
            }

            /// <summary>
            /// Gets the <see cref="IdentifierNameSyntax"/>s found in the scope.
            /// </summary>
            public IReadOnlyList<IdentifierNameSyntax> IdentifierNames => this.identifierNames;

            /// <summary>
            /// Get a walker that has visited <paramref name="node"/>
            /// </summary>
            /// <param name="node">The node</param>
            /// <param name="semanticModel">The <see cref="SemanticModel"/></param>
            /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
            /// <returns>A walker that has visited <paramref name="node"/></returns>
            public static Walker Borrow(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                return BorrowAndVisit(node, Scope.Type, semanticModel, cancellationToken, () => new Walker());
            }

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

            /// <inheritdoc />
            protected override void Clear()
            {
                this.identifierNames.Clear();
            }
        }
    }
}
