namespace Gu.Analyzers
{
    using System;
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
            if (disposable == null ||
                disposable.IsMissing ||
                !IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(disposable, cancellationToken).Type))
            {
                return Result.No;
            }

            using (var pooled = AssignedValueWalker.Create(disposable, semanticModel, cancellationToken))
            {
                return IsCreation(pooled, semanticModel, cancellationToken);
            }
        }

        internal static Result IsAssignedWithCreated(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (field == null ||
                !IsPotentiallyAssignableTo(field.Type))
            {
                return Result.No;
            }

            using (var pooled = AssignedValueWalker.Create(field, semanticModel, cancellationToken))
            {
                return IsCreation(pooled, semanticModel, cancellationToken);
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static Result IsCreation(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken, Pool<AssignedValueWalker>.Pooled pooled = null)
        {
            if (candidate == null ||
                candidate.IsMissing ||
                !IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type))
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
                        return IsEitherCreation(binaryExpression.Left, binaryExpression.Right, pooled, semanticModel, cancellationToken);
                    case SyntaxKind.AsExpression:
                        return IsCreation(binaryExpression.Left, semanticModel, cancellationToken, pooled);
                    default:
                        return Result.Unknown;
                }
            }

            var cast = candidate as CastExpressionSyntax;
            if (cast != null)
            {
                return IsCreation(cast.Expression, semanticModel, cancellationToken, pooled);
            }

            var conditional = candidate as ConditionalExpressionSyntax;
            if (conditional != null)
            {
                return IsEitherCreation(conditional.WhenTrue, conditional.WhenFalse, pooled, semanticModel, cancellationToken);
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

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                if (property.DeclaringSyntaxReferences.Length == 0)
                {
                    return property == KnownSymbol.PasswordBox.SecurePassword
                        ? Result.Yes
                        : Result.No;
                }

                return Result.No;
            }

            var method = symbol as IMethodSymbol;
            if (method != null)
            {
                if (method.DeclaringSyntaxReferences.Length == 0)
                {
                    if (method.ContainingType.Is(KnownSymbol.IDictionary) ||
                        method.ContainingType == KnownSymbol.Enumerable ||
                        method == KnownSymbol.IEnumerable.GetEnumerator ||
                        method.ContainingType.Name.StartsWith("ConditionalWeakTable"))
                    {
                        return Result.No;
                    }

                    return IsAssignableTo(method.ReturnType) ? Result.Maybe : Result.No;
                }

                if (pooled == null)
                {
                    using (pooled = AssignedValueWalker.CreateWithReturnValues(candidate, method, semanticModel, cancellationToken))
                    {
                        return IsCreation(pooled, semanticModel, cancellationToken);
                    }
                }

                return Result.No;
            }

            return Result.Unknown;
        }

        private static Result IsEitherCreation(ExpressionSyntax value1, ExpressionSyntax value2, Pool<AssignedValueWalker>.Pooled pooled, SemanticModel semanticModel, CancellationToken cancellationToken)
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
                switch (IsCreation(value, semanticModel, cancellationToken, pooled))
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

        private static Result IsCreation(Pool<AssignedValueWalker>.Pooled pooled, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var result = Analyzers.Result.No;
            foreach (var assignment in pooled.Item)
            {
                switch (IsCreation(assignment.Value, semanticModel, cancellationToken, pooled))
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
    }
}