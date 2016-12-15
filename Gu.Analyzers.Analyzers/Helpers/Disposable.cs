namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Disposable
    {
        internal enum Source
        {
            Unknown,
            NotDisposable,
            Created,
            PotentiallyCreated,
            Injected,
            Cached
        }

        internal static bool IsPotentiallyAssignedWithCreatedDisposable(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = MemberAssignmentWalker.AssignedValuesInType(field, semanticModel, cancellationToken))
            {
                if (IsAnyADisposableCreation(pooled.Item.AssignedValues, semanticModel, cancellationToken))
                {
                    return true;
                }

                foreach (var assignment in pooled.Item.AssignedValues)
                {
                    var setter = assignment.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
                    if (setter?.IsKind(SyntaxKind.SetAccessorDeclaration) == true)
                    {
                        var property = semanticModel.GetDeclaredSymbol(setter.FirstAncestorOrSelf<PropertyDeclarationSyntax>());
                        if (IsPotentiallyAssignedWithCreatedDisposable(property, semanticModel, cancellationToken))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool IsPotentiallyAssignedWithCreatedDisposable(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = MemberAssignmentWalker.AssignedValuesInType(property, semanticModel, cancellationToken))
            {
                if (IsAnyADisposableCreation(pooled.Item.AssignedValues, semanticModel, cancellationToken))
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
            using (var pooled = Classification.Create(disposable, semanticModel, cancellationToken))
            {
                foreach (var classification in pooled.Item)
                {
                    if (classification.Source == Source.Created ||
                        classification.Source == Source.PotentiallyCreated)
                    {
                        return true;
                    }
                }
            }

            return false;
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

        [DebuggerDisplay("Source: {Source}, Node: {Node}")]
        internal struct Classification
        {
            internal readonly Source Source;
            internal readonly SyntaxNode Node;

            private Classification(Source source, SyntaxNode node)
            {
                this.Source = source;
                this.Node = node;
            }

            internal static Pool<List<Classification>>.Pooled Create(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                using (var pooledSet = SetPool<ExpressionSyntax>.Create())
                {
                    var pooledList = ListPool<Classification>.Create();
                    Check(disposable, semanticModel, cancellationToken, pooledSet.Item, pooledList.Item);
                    return pooledList;
                }
            }

            private static void Check(
                ExpressionSyntax disposable,
                SemanticModel semanticModel,
                CancellationToken cancellationToken,
                HashSet<ExpressionSyntax> @checked,
                List<Classification> classifications)
            {
                if (!@checked.Add(disposable))
                {
                    classifications.Add(new Classification(Source.Unknown, disposable));
                    return;
                }

                if (disposable == null || 
                    disposable is AnonymousFunctionExpressionSyntax ||
                    disposable is LiteralExpressionSyntax)
                {
                    classifications.Add(new Classification(Source.NotDisposable, disposable));
                    return;
                }

                if (disposable.IsKind(SyntaxKind.CoalesceExpression))
                {
                    var binaryExpression = (BinaryExpressionSyntax)disposable;
                    Check(binaryExpression.Left, semanticModel, cancellationToken, @checked, classifications);
                    Check(binaryExpression.Right, semanticModel, cancellationToken, @checked, classifications);
                    return;
                }

                var conditional = disposable as ConditionalExpressionSyntax;
                if (conditional != null)
                {
                    Check(conditional.WhenTrue, semanticModel, cancellationToken, @checked, classifications);
                    Check(conditional.WhenFalse, semanticModel, cancellationToken, @checked, classifications);
                    return;
                }

                var symbol = semanticModel.GetSymbolSafe(disposable, cancellationToken);
                if (symbol == null)
                {
                    classifications.Add(new Classification(Source.Unknown, disposable));
                    return;
                }

                if (disposable is ObjectCreationExpressionSyntax)
                {
                    var source = IsAssignableTo(symbol.ContainingType)
                        ? Source.Created
                        : Source.NotDisposable;
                    classifications.Add(new Classification(source, disposable));

                    return;
                }

                if (symbol is IFieldSymbol)
                {
                    classifications.Add(new Classification(Source.Cached, disposable));
                    return;
                }

                var methodSymbol = symbol as IMethodSymbol;
                if (methodSymbol != null)
                {
                    CheckMethod(disposable, methodSymbol, semanticModel, cancellationToken, @checked, classifications);
                    return;
                }

                var property = symbol as IPropertySymbol;
                if (property != null)
                {
                    CheckProperty(disposable, property, semanticModel, cancellationToken, @checked, classifications);
                    return;
                }

                var local = symbol as ILocalSymbol;
                if (local != null)
                {
                    VariableDeclaratorSyntax variable;
                    if (local.TryGetSingleDeclaration(cancellationToken, out variable) &&
                        variable.Initializer != null)
                    {
                        Check(variable.Initializer.Value, semanticModel, cancellationToken, @checked, classifications);
                        return;
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
                        if (!IsAssignableTo(parameter.Type))
                        {
                            classifications.Add(new Classification(Source.Injected, disposable));
                            return;
                        }

                        var index = ctorSymbol.Parameters.IndexOf(parameter);
                        var type = ctor.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                        foreach (var member in type.Members)
                        {
                            var otherCtor = member as ConstructorDeclarationSyntax;
                            if (otherCtor == null || otherCtor == ctor)
                            {
                                continue;
                            }

                            var ctorArg = otherCtor.Initializer.ArgumentList.Arguments[index].Expression;
                            Check(ctorArg, semanticModel, cancellationToken, @checked, classifications);
                        }

                        using (var pooled = ObjectCreationWalker.Create(type))
                        {
                            foreach (var creation in pooled.Item.ObjectCreations)
                            {
                                if ((creation.Type as IdentifierNameSyntax)?.Identifier.ValueText == ctorSymbol.ContainingType.Name)
                                {
                                    var ctorArg = creation.ArgumentList.Arguments[index].Expression;
                                    Check(ctorArg, semanticModel, cancellationToken, @checked, classifications);
                                }
                            }
                        }

                        return;
                    }
                }

                classifications.Add(new Classification(Source.Injected, disposable));
            }

            private static void CheckMethod(
                ExpressionSyntax disposable,
                IMethodSymbol methodSymbol,
                SemanticModel semanticModel,
                CancellationToken cancellationToken,
                HashSet<ExpressionSyntax> @checked,
                List<Classification> classifications)
            {
                if (methodSymbol.ContainingType == KnownSymbol.Enumerable)
                {
                    classifications.Add(new Classification(Source.Cached, disposable));
                    return;
                }

                if (methodSymbol.DeclaringSyntaxReferences.Length > 0)
                {
                    if (methodSymbol.IsAbstract || methodSymbol.IsVirtual)
                    {
                        using (var pooled = MethodImplementationWalker.Create(methodSymbol, semanticModel, cancellationToken))
                        {
                            if (pooled.Item.Implementations.Any())
                            {
                                foreach (var implementingDeclaration in pooled.Item.Implementations)
                                {
                                    CheckMethod(implementingDeclaration, semanticModel, cancellationToken, @checked, classifications);
                                }

                                return;
                            }
                        }

                        var source = IsAssignableTo(methodSymbol.ReturnType) ? Source.PotentiallyCreated : Source.NotDisposable;
                        classifications.Add(new Classification(source, disposable));
                    }
                    else
                    {
                        foreach (var declaration in methodSymbol.Declarations(cancellationToken))
                        {
                            CheckMethod((MethodDeclarationSyntax)declaration, semanticModel, cancellationToken, @checked, classifications);
                        }
                    }
                }
                else
                {
                    var source = IsAssignableTo(methodSymbol.ReturnType) ? Source.PotentiallyCreated : Source.NotDisposable;
                    classifications.Add(new Classification(source, disposable));
                }
            }

            private static void CheckMethod(
                MethodDeclarationSyntax methodDeclaration,
                SemanticModel semanticModel,
                CancellationToken cancellationToken,
                HashSet<ExpressionSyntax> @checked,
                List<Classification> classifications)
            {
                using (var pooled = ReturnExpressionsWalker.Create(methodDeclaration))
                {
                    foreach (var returnValue in pooled.Item.ReturnValues)
                    {
                        Check(returnValue, semanticModel, cancellationToken, @checked, classifications);
                    }
                }
            }

            private static void CheckProperty(
                ExpressionSyntax disposable,
                IPropertySymbol property,
                SemanticModel semanticModel,
                CancellationToken cancellationToken,
                HashSet<ExpressionSyntax> @checked,
                List<Classification> classifications)
            {
                if (property == KnownSymbol.PasswordBox.SecurePassword)
                {
                    classifications.Add(new Classification(Source.Created, disposable));
                    return;
                }

                if (property.IsAbstract ||
                    property.IsVirtual)
                {
                    using (var pooled = PropertyImplementationWalker.Create(property, semanticModel, cancellationToken))
                    {
                        if (pooled.Item.Implementations.Any())
                        {
                            foreach (var implementingDeclaration in pooled.Item.Implementations)
                            {
                                CheckProperty(implementingDeclaration, semanticModel, cancellationToken, @checked, classifications);
                            }
                        }
                    }

                    return;
                }

                foreach (var propertyDeclaration in property.Declarations(cancellationToken))
                {
                    CheckProperty((PropertyDeclarationSyntax)propertyDeclaration, semanticModel, cancellationToken, @checked, classifications);
                    return;
                }
            }

            private static void CheckProperty(
                PropertyDeclarationSyntax propertyDeclaration,
                SemanticModel semanticModel,
                CancellationToken cancellationToken,
                HashSet<ExpressionSyntax> @checked,
                List<Classification> classifications)
            {
                if (propertyDeclaration.ExpressionBody != null)
                {
                    Check(propertyDeclaration.ExpressionBody.Expression, semanticModel, cancellationToken, @checked, classifications);
                    return;
                }

                AccessorDeclarationSyntax getter;
                if (propertyDeclaration.TryGetGetAccessorDeclaration(out getter))
                {
                    using (var pooled = ReturnExpressionsWalker.Create(getter))
                    {
                        foreach (var returnValue in pooled.Item.ReturnValues)
                        {
                            Check(returnValue, semanticModel, cancellationToken, @checked, classifications);
                        }
                    }

                    return;
                }

                classifications.Add(new Classification(Source.Unknown, propertyDeclaration));
            }
        }
    }
}