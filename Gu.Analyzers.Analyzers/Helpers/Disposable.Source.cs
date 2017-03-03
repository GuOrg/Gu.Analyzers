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
                       IsPotentiallyCachedOrInjectedCore(sources.Item, semanticModel, cancellationToken) == Result.No;
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
                       IsPotentiallyCachedOrInjectedCore(sources.Item, semanticModel, cancellationToken) == Result.No;
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
                       IsPotentiallyCachedOrInjectedCore(sources.Item, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe);
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
                       IsPotentiallyCachedOrInjectedCore(sources.Item, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe);
            }
        }

        /// <summary>
        /// Check if any path returns a created IDisposable
        /// </summary>
        internal static bool IsPotentiallyCachedOrInjected(InvocationExpressionSyntax disposeCall, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            ExpressionSyntax member;
            if (TryGetDisposedRootMember(disposeCall, out member))
            {
                if (IsCachedOrInjectedCore(semanticModel.GetSymbolSafe(member, cancellationToken))
                    .IsEither(Result.Yes, Result.Maybe))
                {
                    return true;
                }

                using (var sources = AssignedValueWalker.Create(member, semanticModel, cancellationToken))
                {
                    return IsPotentiallyCachedOrInjectedCore(sources.Item, semanticModel, cancellationToken).IsEither(Result.Yes, Result.Maybe);
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

            if (IsCachedOrInjectedCore(semanticModel.GetSymbolSafe(disposable, cancellationToken)) == Result.Yes)
            {
                return true;
            }

            return IsPotentiallyCachedOrInjectedCore(disposable, semanticModel, cancellationToken);
        }

        private static bool IsPotentiallyCachedOrInjectedCore(ExpressionSyntax value, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetSymbolSafe(value, cancellationToken);
            if (IsCachedOrInjectedCore(symbol) == Result.Yes)
            {
                return true;
            }

            var property = symbol as IPropertySymbol;
            if (property != null &&
                !property.IsAutoProperty(cancellationToken))
            {
                using (var returnValues = ReturnValueWalker.Create(value, false, semanticModel, cancellationToken))
                {
                    if (IsPotentiallyCachedOrInjectedCore(returnValues.Item, semanticModel, cancellationToken) == Result.Yes)
                    {
                        return true;
                    }
                }
            }
            else
            {
                using (var sources = AssignedValueWalker.Create(value, semanticModel, cancellationToken))
                {
                    if (IsPotentiallyCachedOrInjectedCore(sources.Item, semanticModel, cancellationToken) == Result.Yes)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Result IsPotentiallyCachedOrInjectedCore(IReadOnlyList<ExpressionSyntax> values, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (values.Count == 0)
            {
                return Result.No;
            }

            foreach (var value in values)
            {
                var symbol = semanticModel.GetSymbolSafe(value, cancellationToken);
                if (IsCachedOrInjectedCore(symbol) == Result.Yes)
                {
                    return Result.Yes;
                }
            }

            return Result.No;
        }

        private static Result IsCachedOrInjectedCore(ISymbol symbol)
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
    }
}