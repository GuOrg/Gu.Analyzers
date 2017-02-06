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

        internal static Pool<List<VauleWithSource>>.Pooled GetRecursiveSources(ExpressionSyntax value, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooledSet = SetPool<ExpressionSyntax>.Create())
            {
                var pooledList = ListPool<VauleWithSource>.Create();
                AddRecursively(value, semanticModel, cancellationToken, pooledSet.Item, pooledList.Item);
                return pooledList;
            }
        }

        internal static Pool<List<VauleWithSource>>.Pooled GetRecursiveSources(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooledSet = SetPool<ExpressionSyntax>.Create())
            {
                var pooledList = ListPool<VauleWithSource>.Create();
                using (var pooled = AssignedValueWalker.AssignedValuesInType(field, semanticModel, cancellationToken))
                {
                    foreach (var assignedValue in pooled.Item.AssignedValues)
                    {
                        AddRecursively(assignedValue, semanticModel, cancellationToken, pooledSet.Item, pooledList.Item);
                    }
                }

                return pooledList;
            }
        }

        internal static Pool<List<VauleWithSource>>.Pooled GetRecursiveSources(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (var pooledSet = SetPool<ExpressionSyntax>.Create())
            {
                var pooledList = ListPool<VauleWithSource>.Create();
                using (var pooled = AssignedValueWalker.AssignedValuesInType(property, semanticModel, cancellationToken))
                {
                    foreach (var assignedValue in pooled.Item.AssignedValues)
                    {
                        AddRecursively(assignedValue, semanticModel, cancellationToken, pooledSet.Item, pooledList.Item);
                    }
                }

                return pooledList;
            }
        }

        private static void AddRecursively(
            ExpressionSyntax value,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            HashSet<ExpressionSyntax> @checked,
            List<VauleWithSource> result)
        {
            if (value == null ||
                value.IsMissing)
            {
                result.Add(new VauleWithSource(value, ValueSource.Unknown));
                return;
            }

            if (!@checked.Add(value))
            {
                result.Add(new VauleWithSource(value, ValueSource.Recursion));
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
                AddRecursively(binaryExpression.Left, semanticModel, cancellationToken, @checked, result);
                AddRecursively(binaryExpression.Right, semanticModel, cancellationToken, @checked, result);
                return;
            }

            var conditional = value as ConditionalExpressionSyntax;
            if (conditional != null)
            {
                AddRecursively(conditional.WhenTrue, semanticModel, cancellationToken, @checked, result);
                AddRecursively(conditional.WhenFalse, semanticModel, cancellationToken, @checked, result);
                return;
            }

            var awaitExpression = value as AwaitExpressionSyntax;
            if (awaitExpression != null)
            {
                if (semanticModel.GetSymbolSafe(awaitExpression.Expression, cancellationToken) != null)
                {
                    result.Add(new VauleWithSource(value, ValueSource.Calculated));
                }
                else
                {
                    result.Add(new VauleWithSource(value, ValueSource.Unknown));
                    return;
                }

                AddRecursively(awaitExpression, semanticModel, cancellationToken, @checked, result);
                return;
            }

            var symbol = semanticModel.GetSymbolSafe(value, cancellationToken) ??
                         semanticModel.GetSymbolSafe((value as ElementAccessExpressionSyntax)?.Expression, cancellationToken);
            if (symbol == null)
            {
                var whenNotNull = (value as ConditionalAccessExpressionSyntax)?.WhenNotNull;
                while (whenNotNull is ConditionalAccessExpressionSyntax &&
                       @checked.Add(whenNotNull))
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

            AddRecursively(value, symbol, semanticModel, cancellationToken, @checked, result);
            AddPathRecursively(value, semanticModel, cancellationToken, @checked, result);
        }

        private static void AddPathRecursively(
            ExpressionSyntax value,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            HashSet<ExpressionSyntax> @checked,
            List<VauleWithSource> result)
        {
            var invocation = value as InvocationExpressionSyntax;
            if (invocation != null)
            {
                AddPathRecursively(invocation.Expression, semanticModel, cancellationToken, @checked, result);
                return;
            }

            var expression = (value as MemberAccessExpressionSyntax)?.Expression ??
                             (value as ConditionalAccessExpressionSyntax)?.Expression;
            if (expression == null ||
                expression is ThisExpressionSyntax ||
                expression is BaseExpressionSyntax ||
                @checked.Contains(expression))
            {
                return;
            }

            var symbol = semanticModel.GetSymbolSafe(expression, cancellationToken);
            if (symbol == null ||
                !(symbol is IFieldSymbol || symbol is IPropertySymbol || symbol is IParameterSymbol))
            {
                return;
            }

            if (symbol.IsStatic)
            {
                result.Add(new VauleWithSource(expression, ValueSource.Cached));
                return;
            }

            AddRecursively(expression, semanticModel, cancellationToken, @checked, result);
        }

        private static void AddRecursively(
            AwaitExpressionSyntax awaitExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            HashSet<ExpressionSyntax> @checked,
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
                }
            }

            using (var tempResults = ListPool<VauleWithSource>.Create())
            {
                AddRecursively(awaitedValue, semanticModel, cancellationToken, @checked, tempResults.Item);
                foreach (var temp in tempResults.Item)
                {
                    var symbol = semanticModel.GetSymbolSafe(temp.Value, cancellationToken) ??
                                 semanticModel.GetSymbolSafe((temp.Value as AwaitExpressionSyntax)?.Expression, cancellationToken);
                    if (symbol == null)
                    {
                        result.Add(new VauleWithSource(temp.Value, ValueSource.Unknown));
                        continue;
                    }

                    if (symbol == KnownSymbol.Task.FromResult)
                    {
                        AddRecursively(((InvocationExpressionSyntax)temp.Value).ArgumentList.Arguments[0].Expression, semanticModel, cancellationToken, @checked, result);
                    }
                    else if (symbol == KnownSymbol.Task.Run)
                    {
                        var expression = ((InvocationExpressionSyntax)temp.Value).ArgumentList.Arguments[0].Expression;
                        var lambda = expression as ParenthesizedLambdaExpressionSyntax;
                        if (lambda != null)
                        {
                            AddRecursively(lambda.Body as ExpressionSyntax, semanticModel, cancellationToken, @checked, result);
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

        private static void AddRecursively(
            ExpressionSyntax value,
            ISymbol symbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            HashSet<ExpressionSyntax> @checked,
            List<VauleWithSource> result)
        {
            var method = symbol as IMethodSymbol;
            if (method != null)
            {
                AddReturnValuesRecursively(value, method, semanticModel, cancellationToken, @checked, result);
                return;
            }

            var property = symbol as IPropertySymbol;
            if (property != null)
            {
                AddReturnValuesRecursively(value, property, semanticModel, cancellationToken, @checked, result);
                return;
            }

            var field = symbol as IFieldSymbol;
            if (field != null)
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
                if (!field.IsReadOnly && field.DeclaredAccessibility != Accessibility.Private)
                {
                    result.Add(new VauleWithSource(value, ValueSource.PotentiallyInjected));
                }

                AddAssignedValuesRecursively(field, semanticModel, cancellationToken, @checked, result);
                return;
            }

            var parameter = symbol as IParameterSymbol;
            if (parameter != null)
            {
                AddRecursively(parameter, value, semanticModel, cancellationToken, @checked, result);
                return;
            }

            var variable = symbol as ILocalSymbol;
            if (variable != null)
            {
                AddAssignedValuesRecursively(variable, semanticModel, cancellationToken, @checked, result);
                return;
            }

            result.Add(new VauleWithSource(value, ValueSource.Unknown));
        }

        private static void AddRecursively(
            IParameterSymbol parameter,
            ExpressionSyntax value,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            HashSet<ExpressionSyntax> @checked,
            List<VauleWithSource> result)
        {
            var method = parameter.ContainingSymbol as IMethodSymbol;
            if (method == null)
            {
                result.Add(new VauleWithSource(value, ValueSource.Unknown));
                return;
            }

            if (method.DeclaredAccessibility != Accessibility.Private)
            {
                result.Add(new VauleWithSource(value, ValueSource.Injected));
            }

            using (var calls = CallsWalker.GetCallsInType(method, semanticModel, cancellationToken))
            {
                if (method.AssociatedSymbol == null &&
                    method.MethodKind != MethodKind.LambdaMethod &&
                    method.DeclaredAccessibility == Accessibility.Private &&
                    calls.Item.Invocations.Count == 0 &&
                    calls.Item.ObjectCreations.Count == 0 &&
                    calls.Item.Initializers.Count == 0)
                {
                    result.Add(new VauleWithSource(value, ValueSource.Injected));
                }

                ArgumentSyntax argument;
                foreach (var invocation in calls.Item.Invocations)
                {
                    if (invocation.ArgumentList.TryGetMatchingArgument(parameter, out argument))
                    {
                        AddRecursively(argument.Expression, semanticModel, cancellationToken, @checked, result);
                    }
                }

                foreach (var initializer in calls.Item.Initializers)
                {
                    if (initializer.ArgumentList.TryGetMatchingArgument(parameter, out argument))
                    {
                        AddRecursively(argument.Expression, semanticModel, cancellationToken, @checked, result);
                    }
                }

                foreach (var objectCreation in calls.Item.ObjectCreations)
                {
                    if (objectCreation.ArgumentList.TryGetMatchingArgument(parameter, out argument))
                    {
                        AddRecursively(argument.Expression, semanticModel, cancellationToken, @checked, result);
                    }
                }
            }

            AddAssignedValuesRecursively(parameter, semanticModel, cancellationToken, @checked, result);
        }

        private static void AddAssignedValuesRecursively(
            ISymbol symbol,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            HashSet<ExpressionSyntax> @checked,
            List<VauleWithSource> result)
        {
            using (var assignments = AssignedValueWalker.AssignedValuesInType(symbol, semanticModel, cancellationToken))
            {
                foreach (var assignedValue in assignments.Item.AssignedValues)
                {
                    var argument = assignedValue.FirstAncestor<ArgumentSyntax>();
                    var invocation = argument.FirstAncestor<InvocationExpressionSyntax>();
                    if (invocation != null)
                    {
                        if (semanticModel.GetSymbolSafe(invocation, cancellationToken) == null)
                        {
                            result.Add(new VauleWithSource(invocation, ValueSource.Unknown));
                            continue;
                        }

                        if (argument?.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) == true)
                        {
                            result.Add(new VauleWithSource(invocation, ValueSource.Ref));
                        }
                        else if (argument?.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) == true)
                        {
                            result.Add(new VauleWithSource(invocation, ValueSource.Out));
                        }

                        AddPathRecursively(invocation, semanticModel, cancellationToken, @checked, result);
                        continue;
                    }

                    AddRecursively(assignedValue, semanticModel, cancellationToken, @checked, result);
                }
            }
        }

        private static void AddReturnValuesRecursively(
            ExpressionSyntax value,
            IMethodSymbol method,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            HashSet<ExpressionSyntax> @checked,
            List<VauleWithSource> result)
        {
            if (method.DeclaringSyntaxReferences.Length > 0)
            {
                foreach (var declaration in method.Declarations(cancellationToken))
                {
                    var methodDeclaration = declaration as MethodDeclarationSyntax;
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
                            @checked,
                            result);
                    }
                    else
                    {
                        using (var pooledReturns = ReturnExpressionsWalker.Create(methodDeclaration.Body))
                        {
                            foreach (var returnValue in pooledReturns.Item.ReturnValues)
                            {
                                AddRecursively(returnValue, semanticModel, cancellationToken, @checked, result);
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
            HashSet<ExpressionSyntax> @checked,
            List<VauleWithSource> result)
        {
            if (property.DeclaringSyntaxReferences.Length > 0)
            {
                foreach (var declaration in property.Declarations(cancellationToken))
                {
                    var basePropertyDeclaration = declaration as BasePropertyDeclarationSyntax;
                    if (basePropertyDeclaration == null)
                    {
                        result.Add(new VauleWithSource(value, ValueSource.Unknown));
                        continue;
                    }

                    var propertyDeclaration = declaration as PropertyDeclarationSyntax;
                    if (propertyDeclaration != null)
                    {
                        if (propertyDeclaration.ExpressionBody != null)
                        {
                            result.Add(new VauleWithSource(value, ValueSource.Calculated));
                            AddRecursively(
                                propertyDeclaration.ExpressionBody.Expression,
                                semanticModel,
                                cancellationToken,
                                @checked,
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
                                        if (property.SetMethod.DeclaredAccessibility != Accessibility.Private)
                                        {
                                            result.Add(new VauleWithSource(value, ValueSource.PotentiallyInjected));
                                        }
                                    }

                                    AddAssignedValuesRecursively(property, semanticModel, cancellationToken, @checked, result);
                                }
                                else
                                {
                                    result.Add(new VauleWithSource(value, ValueSource.Calculated));
                                    using (var pooledReturns = ReturnExpressionsWalker.Create(getter.Body))
                                    {
                                        foreach (var returnValue in pooledReturns.Item.ReturnValues)
                                        {
                                            AddRecursively(returnValue, semanticModel, cancellationToken, @checked, result);
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
    }
}