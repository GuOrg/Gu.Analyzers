namespace Gu.Analyzers
{
    using System;
    using System.Threading;

    using Microsoft.CodeAnalysis;
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
                using (var recursive = RecursiveValues.Create(pooled.Item, semanticModel, cancellationToken))
                {
                    return IsAssignedWithCreated(recursive, semanticModel, cancellationToken);
                }
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
                using (var recursive = RecursiveValues.Create(pooled.Item, semanticModel, cancellationToken))
                {
                    return IsAssignedWithCreated(recursive, semanticModel, cancellationToken);
                }
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
                using (var recursive = RecursiveValues.Create(pooled.Item, semanticModel, cancellationToken))
                {
                    return IsAssignedWithCreated(recursive, semanticModel, cancellationToken);
                }
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

            using (var pooled = ReturnValueWalker.Create(candidate, true, semanticModel, cancellationToken))
            {
                if (pooled.Item.Count == 0)
                {
                    var symbol = semanticModel.GetSymbolSafe(candidate, cancellationToken);
                    if (symbol != null && symbol.DeclaringSyntaxReferences.Length == 0)
                    {
                        return IsCreationCore(candidate, semanticModel, cancellationToken);
                    }

                    using (var recursive = RecursiveValues.Create(new[] { candidate }, semanticModel, cancellationToken))
                    {
                        return IsCreationCore(recursive, semanticModel, cancellationToken);
                    }
                }

                using (var recursive = RecursiveValues.Create(pooled.Item, semanticModel, cancellationToken))
                {
                    return IsCreationCore(recursive, semanticModel, cancellationToken);
                }
            }
        }

        private static Result IsAssignedWithCreated(RecursiveValues walker, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (walker.Count == 0)
            {
                return Result.No;
            }

            return IsCreationCore(walker, semanticModel, cancellationToken);
        }

        private static Result IsCreationCore(RecursiveValues values, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            values.Reset();
            var result = Result.No;
            while (values.MoveNext())
            {
                switch (IsCreationCore(values.Current, semanticModel, cancellationToken))
                {
                    case Result.Unknown:
                        if (result == Result.No)
                        {
                            result = Result.Unknown;
                        }

                        break;
                    case Result.Yes:
                        return Result.Yes;
                    case Result.No:
                        break;
                    case Result.Maybe:
                        result = Result.Maybe;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        private static Result IsCreationCore(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null ||
                candidate.IsMissing)
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

            var symbol = semanticModel.GetSymbolSafe(candidate, cancellationToken);
            if (symbol == null ||
                symbol is ILocalSymbol)
            {
                return Result.Unknown;
            }

            if (symbol is IFieldSymbol)
            {
                return Result.No;
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

                return Result.Unknown;
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

                return Result.Unknown;
            }

            return Result.Unknown;
        }
    }
}