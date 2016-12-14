namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Disposable
    {
        internal static bool IsAssignedWithCreatedDisposable(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = MemberAssignmentWalker.Create(field, semanticModel, cancellationToken))
            {
                if (IsAnyADisposableCreation(pooled.Item.Assignments, semanticModel, cancellationToken))
                {
                    return true;
                }

                foreach (var assignment in pooled.Item.Assignments)
                {
                    var setter = assignment.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
                    if (setter?.IsKind(SyntaxKind.SetAccessorDeclaration) == true)
                    {
                        var property = semanticModel.GetDeclaredSymbol(setter.FirstAncestorOrSelf<PropertyDeclarationSyntax>());
                        if (IsAssignedWithCreatedDisposable(property, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsAssignedWithCreatedDisposable(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = MemberAssignmentWalker.Create(property, semanticModel, cancellationToken))
            {
                if (IsAnyADisposableCreation(pooled.Item.Assignments, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static bool IsPotentialCreation(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = SetPool<ExpressionSyntax>.Create())
            {
                return IsPotentialCreation(disposable, semanticModel, cancellationToken, pooled.Item);
            }
        }

        internal static bool IsAssignableTo(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            // https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
            if (type == KnownSymbol.Task)
            {
                return false;
            }

            ITypeSymbol _;
            return type == KnownSymbol.IDisposable ||
                   type.AllInterfaces.TryGetSingle(x => x == KnownSymbol.IDisposable, out _);
        }

        internal static bool TryGetDisposeMethod(ITypeSymbol type, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (type == null)
            {
                return false;
            }

            var disposers = type.GetMembers("Dispose");
            if (disposers.Length == 0)
            {
                return false;
            }

            if (disposers.Length == 1)
            {
                disposeMethod = disposers[0] as IMethodSymbol;
                if (disposeMethod == null)
                {
                    return false;
                }

                return (disposeMethod.Parameters.Length == 0 &&
                        disposeMethod.DeclaredAccessibility == Accessibility.Public) ||
                       (disposeMethod.Parameters.Length == 1 &&
                        disposeMethod.Parameters[0].Type == KnownSymbol.Boolean);
            }

            if (disposers.Length == 2)
            {
                ISymbol temp;
                if (disposers.TryGetSingle(x => (x as IMethodSymbol)?.Parameters.Length == 1, out temp))
                {
                    disposeMethod = temp as IMethodSymbol;
                    return disposeMethod != null &&
                           disposeMethod.Parameters[0].Type == KnownSymbol.Boolean;
                }
            }

            return false;
        }

        internal static bool BaseTypeHasVirtualDisposeMethod(ITypeSymbol type)
        {
            var baseType = type.BaseType;
            while (baseType != null)
            {
                foreach (var member in baseType.GetMembers("Dispose"))
                {
                    var method = member as IMethodSymbol;
                    if (method == null)
                    {
                        continue;
                    }

                    if (member.DeclaredAccessibility == Accessibility.Protected &&
                        member.IsVirtual &&
                        method.Parameters.Length == 1 &&
                        method.Parameters[0].Type == KnownSymbol.Boolean)
                    {
                        return true;
                    }
                }

                baseType = baseType.BaseType;
            }

            return false;
        }

        private static bool IsAnyADisposableCreation(IReadOnlyList<ExpressionSyntax> assignments, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var assignment in assignments)
            {
                if (IsPotentialCreation(assignment, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPotentialCreation(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken, HashSet<ExpressionSyntax> @checked)
        {
            if (!@checked.Add(disposable))
            {
                return false;
            }

            if (disposable == null || disposable is AnonymousFunctionExpressionSyntax)
            {
                return false;
            }

            if (disposable.IsKind(SyntaxKind.CoalesceExpression))
            {
                var binaryExpression = (BinaryExpressionSyntax)disposable;
                return IsPotentialCreation(binaryExpression.Left, semanticModel, cancellationToken, @checked) ||
                       IsPotentialCreation(binaryExpression.Right, semanticModel, cancellationToken, @checked);
            }

            var conditional = disposable as ConditionalExpressionSyntax;
            if (conditional != null)
            {
                return IsPotentialCreation(conditional.WhenTrue, semanticModel, cancellationToken, @checked) ||
                       IsPotentialCreation(conditional.WhenFalse, semanticModel, cancellationToken, @checked);
            }

            var symbol = semanticModel.GetSymbolSafe(disposable, cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            if (disposable is ObjectCreationExpressionSyntax)
            {
                return IsAssignableTo(symbol.ContainingType);
            }

            if (symbol is IFieldSymbol)
            {
                return false;
            }

            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol != null)
            {
                if (methodSymbol.ContainingType == KnownSymbol.Enumerable)
                {
                    return false;
                }

                if (methodSymbol.IsAbstract || methodSymbol.IsVirtual)
                {
                    using (var pooled = MethodImplementationWalker.Create(methodSymbol, semanticModel, cancellationToken))
                    {
                        if (pooled.Item.Implementations.Any())
                        {
                            foreach (var implementingDeclaration in pooled.Item.Implementations)
                            {
                                if (IsPotentialCreation(implementingDeclaration, semanticModel, cancellationToken, @checked))
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    }

                    return IsAssignableTo(methodSymbol.ReturnType);
                }

                if (methodSymbol.DeclaringSyntaxReferences.Length > 0)
                {
                    foreach (var declaration in methodSymbol.Declarations(cancellationToken))
                    {
                        if (IsPotentialCreation((MethodDeclarationSyntax)declaration, semanticModel, cancellationToken, @checked))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return IsAssignableTo(methodSymbol.ReturnType);
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                if (property == KnownSymbol.PasswordBox.SecurePassword)
                {
                    return true;
                }

                if (property.IsAbstract || property.IsVirtual)
                {
                    using (var pooled = PropertyImplementationWalker.Create(property, semanticModel, cancellationToken))
                    {
                        if (pooled.Item.Implementations.Any())
                        {
                            foreach (var implementingDeclaration in pooled.Item.Implementations)
                            {
                                if (IsPotentialCreation(implementingDeclaration, semanticModel, cancellationToken, @checked))
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    }

                    return false;
                }

                foreach (var propertyDeclaration in property.Declarations(cancellationToken))
                {
                    if (IsPotentialCreation((PropertyDeclarationSyntax)propertyDeclaration, semanticModel, cancellationToken, @checked))
                    {
                        return true;
                    }
                }

                return false;
            }

            var local = symbol as ILocalSymbol;
            if (local != null)
            {
                VariableDeclaratorSyntax variable;
                if (local.TryGetSingleDeclaration(cancellationToken, out variable) &&
                    variable.Initializer != null)
                {
                    return IsPotentialCreation(variable.Initializer.Value, semanticModel, cancellationToken, @checked);
                }
            }

            var identifier = disposable as IdentifierNameSyntax;
            var ctor = disposable.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (identifier != null && ctor != null)
            {
                var ctorSymbol = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken);
                IParameterSymbol parameter;
                if (ctorSymbol.DeclaredAccessibility == Accessibility.Private && ctorSymbol.Parameters.TryGetSingle(x => x.Name == identifier.Identifier.ValueText, out parameter))
                {
                    var index = ctorSymbol.Parameters.IndexOf(parameter);
                    var type = ctor.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                    foreach (var member in type.Members)
                    {
                        var otherCtor = member as ConstructorDeclarationSyntax;
                        if (otherCtor == null || otherCtor == ctor)
                        {
                            continue;
                        }

                        if (otherCtor.Initializer == null)
                        {
                            return false;
                        }

                        var chainedCtorSymbol = semanticModel.GetSymbolSafe(otherCtor.Initializer, cancellationToken);
                        if (!ReferenceEquals(chainedCtorSymbol, ctorSymbol))
                        {
                            return false;
                        }

                        var ctorArg = otherCtor.Initializer.ArgumentList.Arguments[index].Expression;
                        if (!IsPotentialCreation(ctorArg, semanticModel, cancellationToken, @checked))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool IsPotentialCreation(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken, HashSet<ExpressionSyntax> @checked)
        {
            using (var pooled = ReturnExpressionsWalker.Create(methodDeclaration))
            {
                foreach (var returnValue in pooled.Item.ReturnValues)
                {
                    if (IsPotentialCreation(returnValue, semanticModel, cancellationToken, @checked))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool IsPotentialCreation(PropertyDeclarationSyntax propertyDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken, HashSet<ExpressionSyntax> @checked)
        {
            if (propertyDeclaration.ExpressionBody != null)
            {
                return IsPotentialCreation(propertyDeclaration.ExpressionBody.Expression, semanticModel, cancellationToken, @checked);
            }

            AccessorDeclarationSyntax getter;
            if (propertyDeclaration.TryGetGetAccessorDeclaration(out getter))
            {
                using (var pooled = ReturnExpressionsWalker.Create(getter))
                {
                    foreach (var returnValue in pooled.Item.ReturnValues)
                    {
                        if (IsPotentialCreation(returnValue, semanticModel, cancellationToken, @checked))
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