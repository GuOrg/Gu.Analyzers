namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
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
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
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

        internal static Injectable CanInject(ObjectCreationExpressionSyntax objectCreation, ConstructorDeclarationSyntax ctor)
        {
            var injectable = Injectable.Safe;
            foreach (var argument in objectCreation.ArgumentList.Arguments)
            {
                var temp = IsInjectable(argument.Expression, ctor);
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

        internal static Injectable IsInjectable(ExpressionSyntax expression, ConstructorDeclarationSyntax ctor)
        {
            var identifierName = expression as IdentifierNameSyntax;
            if (identifierName != null)
            {
                var identifier = identifierName.Identifier.ValueText;
                if (identifier == null)
                {
                    return Injectable.No;
                }

                ParameterSyntax parameter;
                if (!ctor.ParameterList.Parameters.TryGetSingle(x => x.Identifier.ValueText == identifier, out parameter))
                {
                    return Injectable.No;
                }

                return Injectable.Safe;
            }

            var memberAccess = expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                if (memberAccess.Parent is MemberAccessExpressionSyntax ||
                    memberAccess.Expression is MemberAccessExpressionSyntax)
                {
                    // only handling simple servicelocator
                    return Injectable.No;
                }

                return IsInjectable(memberAccess.Expression, ctor);
            }

            var nestedObjectCreation = expression as ObjectCreationExpressionSyntax;
            if (nestedObjectCreation != null)
            {
                if (CanInject(nestedObjectCreation, ctor) == Injectable.No)
                {
                    return Injectable.No;
                }

                return Injectable.Safe;
            }

            return Injectable.No;
        }

        internal static ITypeSymbol MemberType(ISymbol symbol) => (symbol as IPropertySymbol)?.Type;

        private static void HandleObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            if (objectCreation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()?.Modifiers.Any(SyntaxKind.StaticKeyword) != false)
            {
                return;
            }

            var ctor = context.SemanticModel.GetSymbolSafe(objectCreation, context.CancellationToken) as IMethodSymbol;
            if (ctor == null || !IsInjectionType(ctor.ContainingType))
            {
                return;
            }

            if (CanInject(objectCreation, objectCreation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()) == Injectable.Safe)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, objectCreation.GetLocation()));
            }
        }

        private static void HandleMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var ctor = memberAccess.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (ctor?.Modifiers.Any(SyntaxKind.StaticKeyword) != false)
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolSafe(memberAccess, context.CancellationToken);
            var memberType = MemberType(symbol);
            if (memberType == null || !IsInjectionType(memberType))
            {
                return;
            }

            if (IsInjectable(memberAccess, ctor) != Injectable.No)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, memberAccess.GetLocation()));
            }
        }

        private static bool IsInjectionType(ITypeSymbol type)
        {
            if (type.IsValueType ||
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