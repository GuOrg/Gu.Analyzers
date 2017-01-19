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
        private const string Title = "Assign all readonly members.";
        private const string MessageFormat = "Assign all readonly members.";
        private const string Description = "Assign all readonly members.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
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
            context.RegisterSyntaxNodeAction(HandleConstructor, SyntaxKind.ConstructorDeclaration);
        }

        private static void HandleConstructor(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var constructorDeclarationSyntax = (ConstructorDeclarationSyntax)context.Node;
            var ctor = (IMethodSymbol)context.ContainingSymbol;
            if (!ctor.IsStatic && ctor.DeclaredAccessibility == Accessibility.Private)
            {
                return;
            }

            using (var pooled = CtorWalker.Create(constructorDeclarationSyntax, context.SemanticModel, context.CancellationToken))
            {
                if (pooled.Item.Unassigned.Any())
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, constructorDeclarationSyntax.GetLocation()));
                }
            }
        }

        private class CtorWalker : CSharpSyntaxWalker
        {
            private static readonly Pool<CtorWalker> Cache = new Pool<CtorWalker>(
                () => new CtorWalker(),
                x =>
                {
                    x.readOnlies.Clear();
                    x.semanticModel = null;
                    x.cancellationToken = CancellationToken.None;
                });

            private readonly List<string> readOnlies = new List<string>();

            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private CtorWalker()
            {
            }

            public IReadOnlyList<string> Unassigned => this.readOnlies;

            public static Pool<CtorWalker>.Pooled Create(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var pooled = Cache.GetOrCreate();
                pooled.Item.readOnlies.AddRange(ReadOnlies(constructor, semanticModel, cancellationToken));
                pooled.Item.semanticModel = semanticModel;
                pooled.Item.cancellationToken = cancellationToken;
                pooled.Item.Visit(constructor);
                return pooled;
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                IdentifierNameSyntax left;
                if (TryGetIdentifier(node.Left, out left))
                {
                    this.readOnlies.Remove(left.Identifier.ValueText).IgnoreReturnValue();
                }

                base.VisitAssignmentExpression(node);
            }

            public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
            {
                var ctor = this.semanticModel.GetSymbolSafe(node, this.cancellationToken) as IMethodSymbol;
                ConstructorDeclarationSyntax declaration;
                if (ctor.TryGetSingleDeclaration(this.cancellationToken, out declaration))
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
                    var fieldDeclaration = member as FieldDeclarationSyntax;
                    if (fieldDeclaration != null)
                    {
                        var declaration = fieldDeclaration.Declaration;
                        VariableDeclaratorSyntax variable;
                        if (declaration.Variables.TryGetSingle(out variable))
                        {
                            var field = (IFieldSymbol)semanticModel.GetDeclaredSymbolSafe(variable, cancellationToken);
                            if (field.IsReadOnly && field.IsStatic == isStatic && variable.Initializer == null)
                            {
                                yield return field.Name;
                            }
                        }

                        continue;
                    }

                    var propertyDeclaration = member as PropertyDeclarationSyntax;
                    if (propertyDeclaration != null && propertyDeclaration.ExpressionBody == null)
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
        }
    }
}