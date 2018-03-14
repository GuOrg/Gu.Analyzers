namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0004AssignAllReadOnlyMembers : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0004";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Assign all readonly members.",
            messageFormat: "The following readonly members are not assigned:\r\n{0}",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Assign all readonly members.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleConstructor, SyntaxKind.ConstructorDeclaration);
        }

        private static void HandleConstructor(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ConstructorDeclarationSyntax constructorDeclaration &&
                context.ContainingSymbol is IMethodSymbol ctor)
            {
                if (!ctor.IsStatic &&
                     ctor.DeclaredAccessibility == Accessibility.Private)
                {
                    return;
                }

                using (var pooled = CtorWalker.Borrow(constructorDeclaration, context.SemanticModel, context.CancellationToken))
                {
                    if (pooled.Unassigned.Any())
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptor,
                                constructorDeclaration.Identifier.GetLocation(),
                                string.Join(Environment.NewLine, pooled.Unassigned)));
                    }
                }
            }
        }

        private class CtorWalker : PooledWalker<CtorWalker>
        {
            private readonly List<ISymbol> readonlies = new List<ISymbol>();

            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private CtorWalker()
            {
            }

            public IReadOnlyList<ISymbol> Unassigned => this.readonlies;

            public static CtorWalker Borrow(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var walker = Borrow(() => new CtorWalker());
                walker.semanticModel = semanticModel;
                walker.cancellationToken = cancellationToken;
                walker.AddReadOnlies(constructor);
                walker.Visit(constructor);
                return walker;
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (TryGetIdentifier(node.Left, out var identifierName))
                {
                    this.readonlies.Remove(this.semanticModel.GetSymbolSafe(identifierName, this.cancellationToken));
                }

                base.VisitAssignmentExpression(node);
            }

            public override void VisitArgument(ArgumentSyntax node)
            {
                if (TryGetIdentifier(node.Expression, out var identifierName) &&
                    node.RefOrOutKeyword.IsEither(SyntaxKind.RefKeyword, SyntaxKind.OutKeyword))
                {
                    this.readonlies.Remove(this.semanticModel.GetSymbolSafe(identifierName, this.cancellationToken));
                }

                base.VisitArgument(node);
            }

            public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
            {
                var ctor = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                if (ctor.TrySingleDeclaration(this.cancellationToken, out ConstructorDeclarationSyntax declaration))
                {
                    this.Visit(declaration);
                }

                base.VisitConstructorInitializer(node);
            }

            protected override void Clear()
            {
                this.readonlies.Clear();
                this.semanticModel = null;
                this.cancellationToken = CancellationToken.None;
            }

            private void AddReadOnlies(ConstructorDeclarationSyntax ctor)
            {
                var typeDeclarationSyntax = (TypeDeclarationSyntax)ctor.Parent;
                foreach (var member in typeDeclarationSyntax.Members)
                {
                    var isStatic = ctor.Modifiers.Any(SyntaxKind.StaticKeyword);
                    if (member is FieldDeclarationSyntax fieldDeclaration &&
                        fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) &&
                        fieldDeclaration.Declaration.Variables.TryLast(out var last) &&
                        last.Initializer == null &&
                        isStatic == fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        foreach (var variable in fieldDeclaration.Declaration.Variables)
                        {
                            this.readonlies.Add(this.semanticModel.GetDeclaredSymbolSafe(variable, this.cancellationToken));
                        }
                    }
                    else if (member is PropertyDeclarationSyntax propertyDeclaration &&
                             propertyDeclaration.ExpressionBody == null &&
                             !propertyDeclaration.TryGetSetter(out _) &&
                             propertyDeclaration.TryGetGetter(out var getter) &&
                             getter.Body == null &&
                             propertyDeclaration.Initializer == null &&
                             !propertyDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
                             isStatic == propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        this.readonlies.Add(this.semanticModel.GetDeclaredSymbolSafe(propertyDeclaration, this.cancellationToken));
                    }
                }
            }

            private static bool TryGetIdentifier(ExpressionSyntax expression, out IdentifierNameSyntax result)
            {
                result = expression as IdentifierNameSyntax;
                if (result != null)
                {
                    return true;
                }

                if (expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Expression is ThisExpressionSyntax)
                    {
                        return TryGetIdentifier(memberAccess.Name, out result);
                    }

                    if (memberAccess.Expression is IdentifierNameSyntax candidate &&
                        expression.FirstAncestor<TypeDeclarationSyntax>() is TypeDeclarationSyntax typeDeclaration &&
                        candidate.Identifier.ValueText == typeDeclaration.Identifier.ValueText)
                    {
                        return TryGetIdentifier(memberAccess.Name, out result);
                    }
                }

                return false;
            }
        }
    }
}