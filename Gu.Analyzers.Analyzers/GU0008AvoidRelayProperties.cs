namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0008AvoidRelayProperties : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0008";
        private const string Title = "Avoid relay properties.";
        private const string MessageFormat = "Avoid relay properties.";
        private const string Description = "Avoid relay properties.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.DisabledByDefault,
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
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.PropertyDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var propertySymbol = (IPropertySymbol)context.ContainingSymbol;
            if (propertySymbol.IsStatic ||
                propertySymbol.DeclaredAccessibility == Accessibility.Protected ||
                propertySymbol.DeclaredAccessibility == Accessibility.Private)
            {
                return;
            }

            if (IsRelayProperty((PropertyDeclarationSyntax)context.Node, context.SemanticModel, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static bool IsRelayProperty(PropertyDeclarationSyntax property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (property.ExpressionBody != null)
            {
                return IsRelayReturn(property.ExpressionBody.Expression, semanticModel, cancellationToken);
            }

            if (property.TryGetGetAccessorDeclaration(out AccessorDeclarationSyntax getter))
            {
                if (getter.Body == null)
                {
                    return false;
                }

                if (getter.Body.Statements.TryGetSingle(out StatementSyntax statement))
                {
                    return IsRelayReturn((statement as ReturnStatementSyntax)?.Expression, semanticModel, cancellationToken);
                }
            }

            return false;
        }

        private static bool IsRelayReturn(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (expression == null || !expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            var memberAccess = (MemberAccessExpressionSyntax)expression;
            var member = semanticModel.GetSymbolSafe(memberAccess.Expression, cancellationToken);
            if (member == null ||
                !IsInjected(member, semanticModel, cancellationToken))
            {
                return false;
            }

            if (memberAccess.Expression is IdentifierNameSyntax &&
                memberAccess.Name is IdentifierNameSyntax)
            {
                return true;
            }

            if (memberAccess.Expression is MemberAccessExpressionSyntax &&
                memberAccess.Name is IdentifierNameSyntax)
            {
                return true;
            }

            return false;
        }

        private static bool IsInjected(ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (member is IFieldSymbol field)
            {
                using (var walker = AssignedValueWalker.Borrow(field, semanticModel, cancellationToken))
                {
                    foreach (var assignedValue in walker)
                    {
                        if (assignedValue.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() == null)
                        {
                            continue;
                        }

                        if (semanticModel.GetSymbolSafe(assignedValue, cancellationToken) is IParameterSymbol)
                        {
                            return true;
                        }
                    }
                }
            }

            if (member is IPropertySymbol property)
            {
                using (var walker = AssignedValueWalker.Borrow(property, semanticModel, cancellationToken))
                {
                    foreach (var assignedValue in walker)
                    {
                        if (assignedValue.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() == null)
                        {
                            continue;
                        }

                        if (semanticModel.GetSymbolSafe(assignedValue, cancellationToken) is IParameterSymbol)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}