namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0007PreferInjecting : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0007";
        private const string Title = "Prefer injecting.";
        private const string MessageFormat = "Prefer injecting.";
        private const string Description = "Prefer injecting.";
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

        internal enum Injectable
        {
            No,
            Safe,
            Unsafe
        }

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(HandleMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        internal static Injectable CanInject(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (objectCreation?.ArgumentList?.Arguments.Any() != true)
            {
                return Injectable.Safe;
            }

            var injectable = Injectable.Safe;
            foreach (var argument in objectCreation.ArgumentList.Arguments)
            {
                var temp = IsInjectable(argument.Expression, semanticModel, cancellationToken);
                switch (temp)
                {
                    case Injectable.No:
                        return Injectable.No;
                    case Injectable.Safe:
                        break;
                    case Injectable.Unsafe:
                        injectable = Injectable.Unsafe;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return injectable;
        }

        internal static Injectable IsInjectable(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var identifierName = expression as IdentifierNameSyntax;
            if (identifierName?.Identifier != null)
            {
                var identifier = identifierName.Identifier.ValueText;
                if (identifier == null)
                {
                    return Injectable.No;
                }

                if (!TryGetSingleConstructor(expression, out ConstructorDeclarationSyntax ctor))
                {
                    return Injectable.No;
                }

                if (ctor?.Modifiers.Any(SyntaxKind.StaticKeyword) != false)
                {
                    return Injectable.No;
                }

                return Injectable.Safe;
            }

            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Parent is AssignmentExpressionSyntax assignment)
                {
                    if (assignment.Left == expression)
                    {
                        return Injectable.No;
                    }
                }

                if (MemberPath.TryFindRootMember(memberAccess, out ExpressionSyntax rootMember))
                {
                    var rootSymbol = semanticModel.GetSymbolSafe(rootMember, cancellationToken);
                    if (rootSymbol == null)
                    {
                        return Injectable.No;
                    }

                    if (rootSymbol is IParameterSymbol &&
                        memberAccess.FirstAncestor<ConstructorDeclarationSyntax>() != null)
                    {
                        return IsInjectable(memberAccess.Name, semanticModel, cancellationToken);
                    }

                    if (rootSymbol.IsEither<IFieldSymbol, IPropertySymbol>())
                    {
                        return IsInjectable(memberAccess.Name, semanticModel, cancellationToken);
                    }
                }

                return Injectable.No;
            }

            if (expression is ObjectCreationExpressionSyntax nestedObjectCreation)
            {
                if (CanInject(nestedObjectCreation, semanticModel, cancellationToken) == Injectable.No)
                {
                    return Injectable.No;
                }

                return Injectable.Safe;
            }

            return Injectable.No;
        }

        internal static ITypeSymbol MemberType(ISymbol symbol) => (symbol as IPropertySymbol)?.Type;

        internal static bool TryGetSingleConstructor(SyntaxNode node, out ConstructorDeclarationSyntax ctor)
        {
            ctor = null;
            var classDeclaration = node.FirstAncestor<ClassDeclarationSyntax>();
            if (classDeclaration == null)
            {
                return false;
            }

            if (classDeclaration.Members.TryGetSingle(x => x is ConstructorDeclarationSyntax, out MemberDeclarationSyntax single))
            {
                ctor = (ConstructorDeclarationSyntax)single;
                return true;
            }

            return false;
        }

        private static void HandleObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            if (objectCreation.FirstAncestor<ConstructorDeclarationSyntax>()?.Modifiers.Any(SyntaxKind.StaticKeyword) != false)
            {
                return;
            }

            var ctor = context.SemanticModel.GetSymbolSafe(objectCreation, context.CancellationToken) as IMethodSymbol;
            if (ctor == null || !IsInjectionType(ctor.ContainingType))
            {
                return;
            }

            if (CanInject(objectCreation, context.SemanticModel, context.CancellationToken) == Injectable.Safe)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, objectCreation.GetLocation()));
            }
        }

        private static void HandleMemberAccess(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolSafe(memberAccess, context.CancellationToken);
            if (symbol.IsStatic)
            {
                return;
            }

            var memberType = MemberType(symbol);
            if (memberType == null ||
                !IsInjectionType(memberType))
            {
                return;
            }

            if (IsInjectable(memberAccess, context.SemanticModel, context.CancellationToken) != Injectable.No)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, memberAccess.Name.GetLocation()));
            }
        }

        private static bool IsInjectionType(ITypeSymbol type)
        {
            if (type?.ContainingNamespace == null ||
                type.IsValueType ||
                type.IsStatic)
            {
                return false;
            }

            foreach (var namespaceSymbol in type.ContainingNamespace.ConstituentNamespaces)
            {
                if (namespaceSymbol.Name == "System")
                {
                    return false;
                }
            }

            return true;
        }
    }
}