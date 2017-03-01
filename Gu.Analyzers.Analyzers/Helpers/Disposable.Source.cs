namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool IsAssignedWithCreatedAndNotCachedOrInjected(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (field == null ||
                !IsPotentiallyAssignableTo(field.Type))
            {
                return false;
            }

            using (var sources = AssignedValueWalker.Create(field, semanticModel, cancellationToken))
            {
                return IsAssignedWithCreated(sources, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe) &&
                       IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken) == Result.No;
            }
        }

        internal static bool IsAssignedWithCreatedAndNotCachedOrInjected(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (property == null ||
                 !IsPotentiallyAssignableTo(property.Type))
            {
                return false;
            }

            using (var sources = AssignedValueWalker.Create(property, semanticModel, cancellationToken))
            {
                return IsAssignedWithCreated(sources, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe) &&
                       IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken) == Result.No;
            }
        }

        internal static bool IsAssignedWithCreatedAndInjected(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (field == null ||
                !IsPotentiallyAssignableTo(field.Type))
            {
                return false;
            }

            using (var sources = AssignedValueWalker.Create(field, semanticModel, cancellationToken))
            {
                return IsAssignedWithCreated(sources, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe) &&
                       IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe);
            }
        }

        internal static bool IsAssignedWithCreatedAndInjected(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (property == null ||
                !IsPotentiallyAssignableTo(property.Type))
            {
                return false;
            }

            using (var sources = AssignedValueWalker.Create(property, semanticModel, cancellationToken))
            {
                return IsAssignedWithCreated(sources, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe) &&
                       IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe);
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static bool IsPotentiallyCachedOrInjected(ExpressionStatementSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooled = GetDisposedPath(disposeCall, semanticModel, cancellationToken))
            {
                foreach (var value in pooled.Item)
                {
                    if (IsPotentiallyCachedOrInjected(value, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static bool IsPotentiallyCachedOrInjected(ExpressionSyntax disposable, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (disposable == null ||
                disposable.IsMissing ||
                !IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(disposable, cancellationToken).Type))
            {
                return false;
            }

            if (IsCachedOrInjected(semanticModel.GetSymbolSafe(disposable, cancellationToken)) == Result.Yes)
            {
                return true;
            }

            using (var sources = AssignedValueWalker.Create(disposable, semanticModel, cancellationToken))
            {
                return IsPotentiallyCachedOrInjected(sources, semanticModel, cancellationToken) == Result.Yes;
            }
        }

        private static Result IsPotentiallyCachedOrInjected(Pool<AssignedValueWalker>.Pooled pooled, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (pooled.Item.Count == 0)
            {
                return Result.No;
            }

            foreach (var value in pooled.Item)
            {
                var symbol = semanticModel.GetSymbolSafe(value, cancellationToken);
                if (IsCachedOrInjected(symbol) == Result.Yes)
                {
                    return Result.Yes;
                }
            }

            return Result.No;
        }

        private static Result IsCachedOrInjected(ISymbol symbol)
        {
            if (symbol is IParameterSymbol)
            {
                return Result.Yes;
            }

            var field = symbol as IFieldSymbol;
            if (field != null)
            {
                return field.IsStatic ||
                       field.DeclaredAccessibility != Accessibility.Private
                           ? Result.Yes
                           : Result.No;
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                if (property.DeclaredAccessibility != Accessibility.Private &&
                    property.SetMethod != null &&
                    property.SetMethod.DeclaredAccessibility != Accessibility.Private)
                {
                    return Result.Yes;
                }

                return Result.No;
            }

            return Result.No;
        }

        private static bool IsCachedInMember(Pool<List<VauleWithSource>>.Pooled sources, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var vauleWithSource in sources.Item)
            {
                switch (vauleWithSource.Source)
                {
                    case ValueSource.Member:
                        if (IsAssignableTo(semanticModel.GetTypeInfoSafe(vauleWithSource.Value, cancellationToken).Type))
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }
    }
}