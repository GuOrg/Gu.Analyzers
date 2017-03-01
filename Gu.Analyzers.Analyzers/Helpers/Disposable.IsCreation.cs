namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static Result IsAssignedWithCreated(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!IsPotentiallyAssignableTo(disposable, semanticModel, cancellationToken))
            {
                return Result.No;
            }

            using (var pooled = AssignedValueWalker.Create(disposable, semanticModel, cancellationToken))
            {
                return IsAssignedWithCreated(pooled, semanticModel, cancellationToken);
            }
        }

        internal static Result IsAssignedWithCreated(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!IsPotentiallyAssignableTo(field?.Type))
            {
                return Result.No;
            }

            using (var pooled = AssignedValueWalker.Create(field, semanticModel, cancellationToken))
            {
                return IsAssignedWithCreated(pooled, semanticModel, cancellationToken);
            }
        }

        internal static Result IsAssignedWithCreated(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!IsPotentiallyAssignableTo(property?.Type))
            {
                return Result.No;
            }

            using (var pooled = AssignedValueWalker.Create(property, semanticModel, cancellationToken))
            {
                return IsAssignedWithCreated(pooled, semanticModel, cancellationToken);
            }
        }

        internal static Result IsAssignedWithCreated(Pool<AssignedValueWalker>.Pooled pooled, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (pooled.Item.Count == 0)
            {
                return Result.No;
            }

            using (var pooledSet = SetPool<SyntaxNode>.Create())
            {
                return IsCreation(pooled.Item, semanticModel, cancellationToken, pooledSet.Item);
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static Result IsCreation(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!IsPotentiallyAssignableTo(candidate, semanticModel, cancellationToken))
            {
                return Result.No;
            }

            using (var pooled = SetPool<SyntaxNode>.Create())
            {
                return IsCreation(candidate, semanticModel, cancellationToken, pooled.Item);
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        private static Result IsCreation(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, HashSet<SyntaxNode> checkedLocations)
        {
            if (candidate == null ||
                candidate.IsMissing ||
                !checkedLocations.Add(candidate))
            {
                return Result.Unknown;
            }

            if (!IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type))
            {
                return Result.No;
            }

            if (candidate is LiteralExpressionSyntax ||
                candidate is DefaultExpressionSyntax ||
                candidate is TypeOfExpressionSyntax)
            {
                return Result.No;
            }

            if (candidate is ObjectCreationExpressionSyntax ||
                candidate is ArrayCreationExpressionSyntax ||
                candidate is ImplicitArrayCreationExpressionSyntax ||
                candidate is InitializerExpressionSyntax)
            {
                if (IsAssignableTo(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type))
                {
                    return Result.Yes;
                }

                return Result.No;
            }

            var binaryExpression = candidate as BinaryExpressionSyntax;
            if (binaryExpression != null)
            {
                switch (binaryExpression.Kind())
                {
                    case SyntaxKind.CoalesceExpression:
                        return IsEitherCreation(binaryExpression.Left, binaryExpression.Right, semanticModel, cancellationToken, checkedLocations);
                    case SyntaxKind.AsExpression:
                        return IsCreation(binaryExpression.Left, semanticModel, cancellationToken, checkedLocations);
                    default:
                        return Result.Unknown;
                }
            }

            var cast = candidate as CastExpressionSyntax;
            if (cast != null)
            {
                return IsCreation(cast.Expression, semanticModel, cancellationToken, checkedLocations);
            }

            var conditional = candidate as ConditionalExpressionSyntax;
            if (conditional != null)
            {
                return IsEitherCreation(conditional.WhenTrue, conditional.WhenFalse, semanticModel, cancellationToken, checkedLocations);
            }

            var @await = candidate as AwaitExpressionSyntax;
            if (@await != null)
            {
                using (var returnValues = ReturnValueWalker.Create(@await, true, semanticModel, cancellationToken))
                {
                    return IsCreation(returnValues.Item, semanticModel, cancellationToken, checkedLocations);
                }
            }

            var symbol = semanticModel.GetSymbolSafe(candidate, cancellationToken);
            if (symbol == null)
            {
                return Result.Unknown;
            }

            if (symbol is IFieldSymbol)
            {
                return Result.No;
            }

            if (symbol is ILocalSymbol)
            {
                using (var pooled = AssignedValueWalker.Create(candidate, semanticModel, cancellationToken))
                {
                    return IsCreation(pooled.Item, semanticModel, cancellationToken, checkedLocations);
                }
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                if (property.DeclaringSyntaxReferences.Length == 0)
                {
                    return property == KnownSymbol.PasswordBox.SecurePassword
                        ? Result.Yes
                        : Result.No;
                }

                using (var returnValues = ReturnValueWalker.Create(candidate, true, semanticModel, cancellationToken))
                {
                    return IsCreation(returnValues.Item, semanticModel, cancellationToken, checkedLocations);
                }
            }

            var method = symbol as IMethodSymbol;
            if (method != null)
            {
                if (method.DeclaringSyntaxReferences.Length == 0)
                {
                    if (method == KnownSymbol.IEnumerableOfT.GetEnumerator)
                    {
                        return Result.Yes;
                    }

                    if (method.ContainingType.Is(KnownSymbol.IDictionary) ||
                        method.ContainingType == KnownSymbol.Enumerable ||
                        method.ContainingType == KnownSymbol.ConditionalWeakTable ||
                        method == KnownSymbol.IEnumerable.GetEnumerator ||
                        method == KnownSymbol.Task.Run ||
                        method == KnownSymbol.Task.RunOfT ||
                        method == KnownSymbol.Task.ConfigureAwait ||
                        method == KnownSymbol.Task.FromResult)
                    {
                        return Result.No;
                    }

                    return !IsAssignableTo(method.ReturnType) ||
                           method.IsGenericMethod
                               ? Result.No
                               : Result.Maybe;
                }

                using (var returnValues = ReturnValueWalker.Create(candidate, true, semanticModel, cancellationToken))
                {
                    return IsCreation(returnValues.Item, semanticModel, cancellationToken, checkedLocations);
                }
            }

            return Result.Unknown;
        }

        private static Result IsEitherCreation(ExpressionSyntax value1, ExpressionSyntax value2, SemanticModel semanticModel, CancellationToken cancellationToken, HashSet<SyntaxNode> checkedLocations)
        {
            if (value1 == null || value2 == null)
            {
                return Result.No;
            }

            var result = Analyzers.Result.No;
            for (var i = 0; i < 2; i++)
            {
                var value = i == 0
                                ? value1
                                : value2;
                switch (IsCreation(value, semanticModel, cancellationToken, checkedLocations))
                {
                    case Analyzers.Result.Unknown:
                        result = Analyzers.Result.Unknown;
                        break;
                    case Analyzers.Result.Yes:
                        return Result.Yes;
                    case Analyzers.Result.No:
                        break;
                    case Analyzers.Result.Maybe:
                        result = Analyzers.Result.Maybe;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }

        private static Result IsCreation(IReadOnlyList<ExpressionSyntax> values, SemanticModel semanticModel, CancellationToken cancellationToken, HashSet<SyntaxNode> checkedLocations)
        {
            var result = Analyzers.Result.No;
            foreach (var value in values)
            {
                switch (IsCreation(value, semanticModel, cancellationToken, checkedLocations))
                {
                    case Analyzers.Result.Unknown:
                        if (result == Result.No)
                        {
                            result = Analyzers.Result.Unknown;
                        }

                        break;
                    case Analyzers.Result.Yes:
                        return Result.Yes;
                    case Analyzers.Result.No:
                        break;
                    case Analyzers.Result.Maybe:
                        result = Analyzers.Result.Maybe;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }
    }
}