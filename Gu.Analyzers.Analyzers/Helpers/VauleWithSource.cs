namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [DebuggerDisplay("Value: {Value} Source: {Source}")]
    internal struct VauleWithSource
    {
        internal readonly SyntaxNode Value;
        internal readonly ValueSource Source;

        private VauleWithSource(SyntaxNode value, ValueSource source)
        {
            this.Value = value;
            this.Source = source;
        }

        internal static Pool<List<VauleWithSource>>.Pooled GetRecursiveSources(
            ExpressionSyntax value,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var pooledList = ListPool<VauleWithSource>.Create();
            AddRecursively(value, semanticModel, cancellationToken, pooledList.Item);
            return pooledList;
        }

        internal static Pool<List<VauleWithSource>>.Pooled GetRecursiveSources(
            IFieldSymbol field,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var pooledList = ListPool<VauleWithSource>.Create();
            using (var pooled = AssignedValueWalker.Create(field, semanticModel, cancellationToken))
            {
                foreach (var assignedValue in pooled.Item.AssignedValues)
                {
                    AddRecursively(assignedValue, semanticModel, cancellationToken, pooledList.Item);
                }
            }

            return pooledList;
        }

        internal static Pool<List<VauleWithSource>>.Pooled GetRecursiveSources(
            IPropertySymbol property,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var pooledList = ListPool<VauleWithSource>.Create();
            using (var pooled = AssignedValueWalker.Create(property, semanticModel, cancellationToken))
            {
                foreach (var assignedValue in pooled.Item.AssignedValues)
                {
                    AddRecursively(assignedValue, semanticModel, cancellationToken, pooledList.Item);
                }
            }

            return pooledList;
        }

        private static void AddRecursively(
            ExpressionSyntax value,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            List<VauleWithSource> result)
        {
            if (value == null ||
                value.IsMissing)
            {
                result.Add(new VauleWithSource(value, ValueSource.Unknown));
                return;
            }

            int index;
            if (IsAlreadyChecked(value, result, out index))
            {
                result[index] = result[index].WithSource(ValueSource.Recursion);
                return;
            }

            if (value is LiteralExpressionSyntax)
            {
                result.Add(new VauleWithSource(value, ValueSource.Constant));
                return;
            }

            if (value is ObjectCreationExpressionSyntax ||
                value is ArrayCreationExpressionSyntax ||
                value is ImplicitArrayCreationExpressionSyntax ||
                value is InitializerExpressionSyntax)
            {
                result.Add(new VauleWithSource(value, ValueSource.Created));
                return;
            }

            if (value.IsKind(SyntaxKind.CoalesceExpression))
            {
                var binaryExpression = (BinaryExpressionSyntax)value;
                AddRecursively(binaryExpression.Left, semanticModel, cancellationToken, result);
                AddRecursively(binaryExpression.Right, semanticModel, cancellationToken, result);
                return;
            }

            var conditional = value as ConditionalExpressionSyntax;
            if (conditional != null)
            {
                AddRecursively(conditional.WhenTrue, semanticModel, cancellationToken, result);
                AddRecursively(conditional.WhenFalse, semanticModel, cancellationToken, result);
                return;
            }

            var awaitExpression = value as AwaitExpressionSyntax;
            if (awaitExpression != null)
            {
                AddRecursively(awaitExpression, semanticModel, cancellationToken, result);
                return;
            }

            var argument = value.FirstAncestor<ArgumentSyntax>();
            var invocation = argument?.FirstAncestor<InvocationExpressionSyntax>();
            if (invocation != null)
            {
                if (semanticModel.GetSymbolSafe(invocation, cancellationToken) == null)
                {
                    result.Add(new VauleWithSource(invocation, ValueSource.Unknown));
                }

                if (argument.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword))
                {
                    result.Add(new VauleWithSource(invocation, ValueSource.Ref));
                }
                else if (argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    result.Add(new VauleWithSource(invocation, ValueSource.Out));
                }
            }

            var symbol = semanticModel.GetSymbolSafe(value, cancellationToken) ??
                         semanticModel.GetSymbolSafe(
                             (value as ElementAccessExpressionSyntax)?.Expression,
                             cancellationToken);
            if (symbol == null)
            {
                var whenNotNull = (value as ConditionalAccessExpressionSyntax)?.WhenNotNull;
                while (whenNotNull is ConditionalAccessExpressionSyntax && !IsAlreadyChecked(whenNotNull, result, out index))
                {
                    whenNotNull = ((ConditionalAccessExpressionSyntax)whenNotNull).WhenNotNull;
                }

                symbol = semanticModel.GetSymbolSafe(whenNotNull, cancellationToken);
                if (symbol == null)
                {
                    result.Add(new VauleWithSource(value, ValueSource.Unknown));
                    return;
                }
            }

            var method = symbol as IMethodSymbol;
            if (method != null)
            {
                AddReturnValuesRecursively(value, method, semanticModel, cancellationToken, result);
                AddPathRecursively(value, semanticModel, cancellationToken, result);
                return;
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                AddReturnValuesRecursively(value, property, semanticModel, cancellationToken, result);
                AddPathRecursively(value, semanticModel, cancellationToken, result);
                return;
            }

            var field = symbol as IFieldSymbol;
            if (field != null)
            {
                AddReturnValuesRecursively(value, field, semanticModel, cancellationToken, result);
                AddPathRecursively(value, semanticModel, cancellationToken, result);
                return;
            }

            var parameter = symbol as IParameterSymbol;
            if (parameter != null)
            {
                AddRecursively(value, parameter, semanticModel, cancellationToken, result);
                return;
            }

            var variable = symbol as ILocalSymbol;
            if (variable != null)
            {
                AddAssignedValuesRecursively(value, semanticModel, cancellationToken, result);
                return;
            }

            result.Add(new VauleWithSource(value, ValueSource.Unknown));
        }

        private static bool IsAlreadyChecked(ExpressionSyntax value, List<VauleWithSource> result)
        {
            int index;
            return IsAlreadyChecked(value, result, out index);
        }

        private static bool IsAlreadyChecked(ExpressionSyntax value, List<VauleWithSource> result, out int index)
        {
            index = -1;
            for (var i = 0; i < result.Count; i++)
            {
                var vauleWithSource = result[i];
                if (ReferenceEquals(vauleWithSource.Value, value))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private static void AddPathRecursively(
            ExpressionSyntax value,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            List<VauleWithSource> result)
        {
            var invocation = value as InvocationExpressionSyntax;
            if (invocation != null)
            {
                var method = semanticModel.GetSymbolSafe(invocation, cancellationToken);
                if (method == null ||
                    method.IsStatic)
                {
                    return;
                }

                AddPathRecursively(invocation.Expression, semanticModel, cancellationToken, result);
                return;
            }

            var expression = (value as MemberAccessExpressionSyntax)?.Expression ??
                             (value as ConditionalAccessExpressionSyntax)?.Expression;
            int index;
            if (expression == null ||
                expression is ThisExpressionSyntax ||
                expression is BaseExpressionSyntax ||
                IsAlreadyChecked(expression, result, out index))
            {
                return;
            }

            var symbol = semanticModel.GetSymbolSafe(expression, cancellationToken);
            if (symbol == null)
            {
                return;
            }

            if (symbol.IsStatic)
            {
                result.Add(new VauleWithSource(expression, ValueSource.Cached));
                return;
            }

            AddRecursively(expression, semanticModel, cancellationToken, result);
        }

        private static void AddRecursively(
            AwaitExpressionSyntax awaitExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            List<VauleWithSource> result)
        {
            var awaitedValue = awaitExpression.Expression;
            var invocation = awaitedValue as InvocationExpressionSyntax;
            if (invocation != null)
            {
                var awaitedSymbol = semanticModel.GetSymbolSafe(awaitedValue, cancellationToken);
                if (awaitedSymbol?.Name == "ConfigureAwait")
                {
                    awaitedValue = invocation.Expression;
                    var memberAccess = awaitedValue as MemberAccessExpressionSyntax;
                    if (memberAccess != null)
                    {
                        awaitedValue = memberAccess.Expression;
                    }

                    awaitedSymbol = semanticModel.GetSymbolSafe(awaitedValue, cancellationToken);
                }

                if (awaitedSymbol != null)
                {
                    var source = awaitedSymbol.DeclaringSyntaxReferences.Length == 0
                                     ? ValueSource.External
                                     : ValueSource.Calculated;
                    result.Add(new VauleWithSource(awaitExpression, source));
                }
                else
                {
                    result.Add(new VauleWithSource(awaitExpression, ValueSource.Unknown));
                    return;
                }
            }

            using (var tempResults = ListPool<VauleWithSource>.Create())
            {
                tempResults.Item.AddRange(result);
                AddRecursively(awaitedValue, semanticModel, cancellationToken, tempResults.Item);
                for (var i = 0; i < result.Count; i++)
                {
                    if (tempResults.Item[i].Source == ValueSource.Recursion)
                    {
                        result[i] = tempResults.Item[i];
                    }
                }

                for (var i = result.Count; i < tempResults.Item.Count; i++)
                {
                    var temp = tempResults.Item[i];
                    var symbol = semanticModel.GetSymbolSafe(temp.Value, cancellationToken) ??
                                 semanticModel.GetSymbolSafe(
                                     (temp.Value as AwaitExpressionSyntax)?.Expression,
                                     cancellationToken);
                    if (symbol == null)
                    {
                        result.Add(new VauleWithSource(temp.Value, ValueSource.Unknown));
                        continue;
                    }

                    if (symbol == KnownSymbol.Task.FromResult)
                    {
                        AddRecursively(
                            ((InvocationExpressionSyntax)temp.Value).ArgumentList.Arguments[0].Expression,
                            semanticModel,
                            cancellationToken,
                            result);
                    }
                    else if (symbol == KnownSymbol.Task.Run)
                    {
                        var expression = ((InvocationExpressionSyntax)temp.Value).ArgumentList.Arguments[0].Expression;
                        var lambda = expression as ParenthesizedLambdaExpressionSyntax;
                        if (lambda != null)
                        {
                            AddRecursively(lambda.Body as ExpressionSyntax, semanticModel, cancellationToken, result);
                        }
                        else
                        {
                            result.Add(new VauleWithSource(expression, ValueSource.Unknown));
                        }
                    }
                    else
                    {
                        result.Add(temp);
                    }
                }
            }
        }

        private static void AddReturnValuesRecursively(
            ExpressionSyntax value,
            IMethodSymbol method,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            List<VauleWithSource> result)
        {
            if (method.DeclaringSyntaxReferences.Length > 0)
            {
                if (value.FirstAncestor<AwaitExpressionSyntax>() == null)
                {
                    result.Add(new VauleWithSource(value, ValueSource.Calculated));
                }

                foreach (var reference in method.DeclaringSyntaxReferences)
                {
                    var methodDeclaration = reference.GetSyntax(cancellationToken) as MethodDeclarationSyntax;
                    if (methodDeclaration == null)
                    {
                        result.Add(new VauleWithSource(value, ValueSource.Unknown));
                        continue;
                    }

                    if (methodDeclaration.ExpressionBody != null)
                    {
                        AddRecursively(
                            methodDeclaration.ExpressionBody.Expression,
                            semanticModel,
                            cancellationToken,
                            result);
                    }
                    else
                    {
                        using (var pooledReturns = ReturnExpressionsWalker.Create(methodDeclaration.Body))
                        {
                            foreach (var returnValue in pooledReturns.Item.ReturnValues)
                            {
                                AddRecursively(returnValue, semanticModel, cancellationToken, result);
                            }
                        }
                    }
                }
            }
            else
            {
                result.Add(new VauleWithSource(value, ValueSource.External));
            }
        }

        private static void AddReturnValuesRecursively(
            ExpressionSyntax value,
            IPropertySymbol property,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            List<VauleWithSource> result)
        {
            if (property.DeclaringSyntaxReferences.Length > 0)
            {
                foreach (var reference in property.DeclaringSyntaxReferences)
                {
                    var basePropertyDeclaration = reference.GetSyntax(cancellationToken) as BasePropertyDeclarationSyntax;
                    if (basePropertyDeclaration == null)
                    {
                        result.Add(new VauleWithSource(value, ValueSource.Unknown));
                        continue;
                    }

                    var propertyDeclaration = reference.GetSyntax(cancellationToken) as PropertyDeclarationSyntax;
                    if (propertyDeclaration != null)
                    {
                        if (propertyDeclaration.ExpressionBody != null)
                        {
                            result.Add(new VauleWithSource(value, ValueSource.Calculated));
                            AddRecursively(
                                propertyDeclaration.ExpressionBody.Expression,
                                semanticModel,
                                cancellationToken,
                                result);
                        }
                        else
                        {
                            AccessorDeclarationSyntax getter;
                            if (propertyDeclaration.TryGetGetAccessorDeclaration(out getter))
                            {
                                if (getter.Body == null)
                                {
                                    result.Add(new VauleWithSource(value, ValueSource.Member));
                                    AccessorDeclarationSyntax setter;
                                    if (propertyDeclaration.TryGetSetAccessorDeclaration(out setter) &&
                                        setter.Body == null)
                                    {
                                        VauleWithSource first;
                                        if (property.SetMethod.DeclaredAccessibility != Accessibility.Private &&
                                            result.TryGetFirst(out first) &&
                                            first.Value.FirstAncestor<ConstructorDeclarationSyntax>() == null)
                                        {
                                            result.Add(new VauleWithSource(value, ValueSource.PotentiallyInjected));
                                        }
                                    }

                                    AddAssignedValuesRecursively(value, semanticModel, cancellationToken, result);
                                }
                                else
                                {
                                    result.Add(new VauleWithSource(value, ValueSource.Calculated));
                                    using (var pooledReturns = ReturnExpressionsWalker.Create(getter.Body))
                                    {
                                        foreach (var returnValue in pooledReturns.Item.ReturnValues)
                                        {
                                            AddRecursively(returnValue, semanticModel, cancellationToken, result);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                result.Add(new VauleWithSource(value, ValueSource.External));
            }
        }

        private static void AddReturnValuesRecursively(
            ExpressionSyntax value,
            IFieldSymbol field,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            List<VauleWithSource> result)
        {
            if (field.IsStatic)
            {
                result.Add(new VauleWithSource(value, ValueSource.Cached));
                return;
            }

            if (field.IsConst)
            {
                result.Add(new VauleWithSource(value, ValueSource.Constant));
                return;
            }

            result.Add(new VauleWithSource(value, ValueSource.Member));
            VauleWithSource first;
            if (!field.IsReadOnly &&
                field.DeclaredAccessibility != Accessibility.Private &&
                result.TryGetFirst(out first) &&
                first.Value.FirstAncestor<ConstructorDeclarationSyntax>() == null)
            {
                result.Add(new VauleWithSource(value, ValueSource.PotentiallyInjected));
            }

            AddAssignedValuesRecursively(value, semanticModel, cancellationToken, result);
        }

        private static void AddRecursively(
            ExpressionSyntax value,
            IParameterSymbol parameter,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            List<VauleWithSource> result)
        {
            var method = parameter.ContainingSymbol as IMethodSymbol;
            if (method == null)
            {
                result.Add(new VauleWithSource(value, ValueSource.Unknown));
                return;
            }

            bool wasCalled = false;
            for (var i = result.Count - 1; i >= 0; i--)
            {
                var vauleWithSource = result[i];
                var invocation = vauleWithSource.Value as InvocationExpressionSyntax;
                if (invocation != null &&
                    ReferenceEquals(semanticModel.GetSymbolSafe(invocation, cancellationToken), method))
                {
                    wasCalled = true;
                    ExpressionSyntax argumentValue;
                    if (invocation.ArgumentList.TryGetMatchingArgumentValue(parameter, cancellationToken, out argumentValue))
                    {
                        if (!IsAlreadyChecked(value, result))
                        {
                            result.Add(new VauleWithSource(value, ValueSource.Argument));
                            AddAssignedValuesRecursively(value, semanticModel, cancellationToken, result);
                            AddRecursively(argumentValue, semanticModel, cancellationToken, result);
                        }
                    }
                    else
                    {
                        result.Add(new VauleWithSource(value, ValueSource.Unknown));
                    }
                }
            }

            if (!wasCalled)
            {
                using (var calls = CallsWalker.GetCallsInType(method, semanticModel, cancellationToken))
                {
                    if (method.AssociatedSymbol == null &&
                        method.MethodKind != MethodKind.LambdaMethod &&
                        calls.Item.Invocations.Count == 0 &&
                        calls.Item.ObjectCreations.Count == 0 &&
                        calls.Item.Initializers.Count == 0)
                    {
                        result.Add(new VauleWithSource(value, ValueSource.Injected));
                    }
                    else if (method.AssociatedSymbol == null &&
                             method.MethodKind != MethodKind.LambdaMethod &&
                             method.DeclaredAccessibility != Accessibility.Private)
                    {
                        result.Add(new VauleWithSource(value, ValueSource.PotentiallyInjected));
                    }
                    else
                    {
                        result.Add(new VauleWithSource(value, ValueSource.Argument));
                    }

                    ExpressionSyntax argumentValue;
                    foreach (var call in calls.Item.Invocations)
                    {
                        if (call.ArgumentList.TryGetMatchingArgumentValue(parameter, cancellationToken, out argumentValue))
                        {
                            AddRecursively(argumentValue, semanticModel, cancellationToken, result);
                        }
                        else
                        {
                            result.Add(new VauleWithSource(value, ValueSource.Unknown));
                        }
                    }

                    foreach (var initializer in calls.Item.Initializers)
                    {
                        if (initializer.ArgumentList.TryGetMatchingArgumentValue(parameter, cancellationToken, out argumentValue))
                        {
                            AddRecursively(argumentValue, semanticModel, cancellationToken, result);
                        }
                        else
                        {
                            result.Add(new VauleWithSource(value, ValueSource.Unknown));
                        }
                    }

                    foreach (var objectCreation in calls.Item.ObjectCreations)
                    {
                        if (objectCreation.ArgumentList.TryGetMatchingArgumentValue(parameter, cancellationToken, out argumentValue))
                        {
                            AddRecursively(argumentValue, semanticModel, cancellationToken, result);
                        }
                        else
                        {
                            result.Add(new VauleWithSource(value, ValueSource.Unknown));
                        }
                    }
                }
            }
        }

        private static void AddAssignedValuesRecursively(
            ExpressionSyntax value,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            List<VauleWithSource> result)
        {
            using (var assignments = AssignedValueWalker.Create(value, semanticModel, cancellationToken))
            {
                foreach (var assignedValue in assignments.Item.AssignedValues)
                {
                    AddRecursively(assignedValue, semanticModel, cancellationToken, result);
                }
            }
        }

        private VauleWithSource WithSource(ValueSource source)
        {
            return new VauleWithSource(this.Value, source);
        }
    }
}