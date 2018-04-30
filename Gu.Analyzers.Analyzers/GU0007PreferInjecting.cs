namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0007PreferInjecting : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0007";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Prefer injecting.",
            messageFormat: "Prefer injecting.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.DisabledByDefault,
            description: "Prefer injecting.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

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

                if (!TrySingleConstructor(expression, out var ctor))
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
                if (memberAccess.Parent is AssignmentExpressionSyntax assignment &&
                    assignment.Left == expression)
                {
                    return Injectable.No;
                }

                if (MemberPath.TryFindRoot(memberAccess, out var rootMember) &&
                    semanticModel.TryGetSymbol(rootMember, cancellationToken, out ISymbol rootSymbol))
                {
                    switch (rootSymbol)
                    {
                        case IParameterSymbol _:
                        case IFieldSymbol _:
                        case IPropertySymbol _:
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

        internal static ITypeSymbol MemberType(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetSymbolSafe(memberAccess, cancellationToken);
            if (symbol == null ||
                symbol.IsStatic)
            {
                return null;
            }

            if (symbol is IPropertySymbol property)
            {
                if (!property.Type.IsSealed &&
                    !property.Type.IsValueType &&
                    AssignedType(symbol, semanticModel, cancellationToken, out var memberType))
                {
                    return memberType;
                }

                return property.Type;
            }

            if (symbol is IFieldSymbol field)
            {
                if (!field.Type.IsSealed &&
                    !field.Type.IsValueType &&
                    AssignedType(symbol, semanticModel, cancellationToken, out var memberType))
                {
                    return memberType;
                }

                return field.Type;
            }

            return null;
        }

        internal static bool TrySingleConstructor(SyntaxNode node, out ConstructorDeclarationSyntax ctor)
        {
            ctor = null;
            var classDeclaration = node.FirstAncestor<ClassDeclarationSyntax>();
            if (classDeclaration == null)
            {
                return false;
            }

            if (classDeclaration.Members.TrySingle(x => x is ConstructorDeclarationSyntax, out var single))
            {
                ctor = (ConstructorDeclarationSyntax)single;
                return true;
            }

            return false;
        }

        internal static bool IsRootValid(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (MemberPath.TryFindRoot(memberAccess, out var root))
            {
                var symbol = semanticModel.GetSymbolSafe(root, cancellationToken);
                if (symbol is IParameterSymbol parameter)
                {
                    if (parameter.IsParams)
                    {
                        return false;
                    }

                    if (parameter.ContainingSymbol is IMethodSymbol method)
                    {
                        switch (method.MethodKind)
                        {
                            case MethodKind.AnonymousFunction:
                            case MethodKind.Conversion:
                            case MethodKind.DelegateInvoke:
                            case MethodKind.Destructor:
                            case MethodKind.EventAdd:
                            case MethodKind.EventRaise:
                            case MethodKind.EventRemove:
                            case MethodKind.UserDefinedOperator:
                            case MethodKind.ReducedExtension:
                            case MethodKind.StaticConstructor:
                            case MethodKind.BuiltinOperator:
                            case MethodKind.DeclareMethod:
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        private static void HandleObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis() ||
                context.ContainingSymbol.IsStatic)
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                !context.ContainingSymbol.IsStatic &&
                TrySingleConstructor(objectCreation, out var contextCtor) &&
                !contextCtor.Modifiers.Any(SyntaxKind.PrivateKeyword) &&
                CanInject(objectCreation, context.SemanticModel, context.CancellationToken) == Injectable.Safe &&
                context.SemanticModel.GetSymbolSafe(objectCreation, context.CancellationToken) is IMethodSymbol ctor &&
                IsInjectionType(ctor.ContainingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, objectCreation.GetLocation()));
            }
        }

        private static void HandleMemberAccess(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis() ||
                context.ContainingSymbol.IsStatic)
            {
                return;
            }

            if (context.Node is MemberAccessExpressionSyntax memberAccess &&
                !context.ContainingSymbol.IsStatic &&
                TrySingleConstructor(memberAccess, out var contextCtor) &&
                !contextCtor.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                if (memberAccess.Parent is AssignmentExpressionSyntax assignment &&
                    assignment.Left == memberAccess)
                {
                    return;
                }

                if (memberAccess.Expression is ThisExpressionSyntax ||
                    memberAccess.Expression is BaseExpressionSyntax)
                {
                    return;
                }

                if (!IsRootValid(memberAccess, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                var memberType = MemberType(memberAccess, context.SemanticModel, context.CancellationToken);
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
        }

        private static bool AssignedType(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken, out ITypeSymbol memberType)
        {
            foreach (var reference in symbol.DeclaringSyntaxReferences)
            {
                var node = reference.GetSyntax(cancellationToken);

                if (AssignmentExecutionWalker.SingleForSymbol(
                        symbol,
                        node.FirstAncestor<TypeDeclarationSyntax>(),
                        Search.TopLevel,
                        semanticModel,
                        cancellationToken,
                        out var assignment) &&
                    assignment.Right is IdentifierNameSyntax identifier)
                {
                    var ctor = assignment.FirstAncestor<ConstructorDeclarationSyntax>();
                    if (ctor != null &&
                        ctor.ParameterList != null &&
                        ctor.ParameterList.Parameters.TryFirst(
                            p => p.Identifier.ValueText == identifier.Identifier.ValueText,
                            out var parameter))
                    {
                        memberType = semanticModel.GetDeclaredSymbolSafe(parameter, cancellationToken)?.Type;
                        return true;
                    }
                }
            }

            memberType = null;
            return false;
        }

        private static bool IsInjectionType(ITypeSymbol type)
        {
            if (type?.ContainingNamespace == null ||
                type.IsValueType ||
                type.IsStatic ||
                type.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.Constructors.Length != 1)
                {
                    return false;
                }

                var ctor = namedType.Constructors[0];
                if (ctor.Parameters.Length == 0)
                {
                    return true;
                }

                if (ctor.Parameters[ctor.Parameters.Length - 1]
                        .IsParams)
                {
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
