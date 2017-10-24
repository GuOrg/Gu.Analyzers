namespace Gu.Analyzers
{
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
            messageFormat: "The following readonly members are not assigned: {0}.",
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
                                string.Join(", ", pooled.Unassigned)));
                    }
                }
            }
        }

        private class CtorWalker : PooledWalker<CtorWalker>
        {
            private readonly List<string> readOnlies = new List<string>();

            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private CtorWalker()
            {
            }

            public IReadOnlyList<string> Unassigned => this.readOnlies;

            public static CtorWalker Borrow(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var walker = Borrow(() => new CtorWalker());
                walker.readOnlies.AddRange(ReadOnlies(constructor, semanticModel, cancellationToken));
                walker.semanticModel = semanticModel;
                walker.cancellationToken = cancellationToken;
                walker.Visit(constructor);
                return walker;
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (TryGetIdentifier(node.Left, out IdentifierNameSyntax left))
                {
                    this.readOnlies.Remove(left.Identifier.ValueText)
                        .IgnoreReturnValue();
                }

                base.VisitAssignmentExpression(node);
            }

            public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
            {
                var ctor = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                if (ctor.TryGetSingleDeclaration(this.cancellationToken, out ConstructorDeclarationSyntax declaration))
                {
                    this.Visit(declaration);
                }

                base.VisitConstructorInitializer(node);
            }

            private static IEnumerable<string> ReadOnlies(ConstructorDeclarationSyntax ctor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var isStatic = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken)
                                            .IsStatic;
                var typeDeclarationSyntax = (TypeDeclarationSyntax)ctor.Parent;
                foreach (var member in typeDeclarationSyntax.Members)
                {
                    if (member is FieldDeclarationSyntax fieldDeclaration)
                    {
                        var declaration = fieldDeclaration.Declaration;
                        if (declaration.Variables.TryGetSingle(out VariableDeclaratorSyntax variable))
                        {
                            var field = (IFieldSymbol)semanticModel.GetDeclaredSymbolSafe(variable, cancellationToken);
                            if (field.IsReadOnly &&
                                field.IsStatic == isStatic &&
                                variable.Initializer == null)
                            {
                                yield return field.Name;
                            }
                        }

                        continue;
                    }

                    var propertyDeclaration = member as PropertyDeclarationSyntax;
                    if (propertyDeclaration != null &&
                        propertyDeclaration.ExpressionBody == null &&
                        propertyDeclaration.TryGetGetAccessorDeclaration(out AccessorDeclarationSyntax getter) &&
                        getter.Body == null)
                    {
                        var property = semanticModel.GetDeclaredSymbolSafe(propertyDeclaration, cancellationToken);
                        if (property.IsReadOnly &&
                            property.IsStatic == isStatic &&
                            !property.IsAbstract &&
                            propertyDeclaration.Initializer == null)
                        {
                            yield return propertyDeclaration.Identifier.ValueText;
                        }
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

                var member = expression as MemberAccessExpressionSyntax;
                if (member?.Expression is ThisExpressionSyntax)
                {
                    return TryGetIdentifier(member.Name, out result);
                }

                return false;
            }

            protected override void Clear()
            {
                this.readOnlies.Clear();
                this.semanticModel = null;
                this.cancellationToken = CancellationToken.None;
            }
        }
    }
}