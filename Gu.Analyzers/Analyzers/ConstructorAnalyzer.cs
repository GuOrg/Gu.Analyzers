namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ConstructorAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0003CtorParameterNamesShouldMatch,
            Descriptors.GU0004AssignAllReadOnlyMembers,
            Descriptors.GU0014PreferParameter);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ConstructorDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is ConstructorDeclarationSyntax constructorDeclaration)
            {
                using (var walker = CtorWalker.Borrow(constructorDeclaration, context.SemanticModel, context.CancellationToken))
                {
                    if (constructorDeclaration.ParameterList is ParameterListSyntax parameterList &&
                        parameterList.Parameters.Count > 0)
                    {
                        foreach (var parameter in parameterList.Parameters)
                        {
                            if (ShouldRename(parameter, walker, context, out var name))
                            {
                                var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", name), });
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0003CtorParameterNamesShouldMatch, parameter.Identifier.GetLocation(), properties));
                            }
                        }

                        foreach (var assignment in walker.Assignments)
                        {
                            if (constructorDeclaration.Contains(assignment) &&
                               TryGetIdentifier(assignment.Left, out var left))
                            {
                                if (TryGetIdentifier(assignment.Right, out var right) &&
                                    parameterList.Parameters.TryFirst(x => x.Identifier.ValueText == right.Identifier.ValueText, out var parameter) &&
                                    !parameter.Modifiers.Any(SyntaxKind.ParamsKeyword) &&
                                    walker.Assignments.TrySingle(x => x.Right is IdentifierNameSyntax id && id.Identifier.ValueText == parameter.Identifier.ValueText, out _) &&
                                    context.SemanticModel.TryGetSymbol(left, context.CancellationToken, out ISymbol leftSymbol))
                                {
                                    foreach (var argument in walker.Arguments)
                                    {
                                        if (argument.Parent is ArgumentListSyntax al &&
                                            al.Parent is InvocationExpressionSyntax invocation &&
                                            invocation.TryGetMethodName(out var methodName) &&
                                            methodName == "nameof")
                                        {
                                            continue;
                                        }

                                        if (ShouldUseParameter(context, leftSymbol, argument.Expression))
                                        {
                                            var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", parameter.Identifier.ValueText), });
                                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, argument.Expression.GetLocation(), properties));
                                        }
                                    }

                                    foreach (var invocation in walker.Invocations)
                                    {
                                        if (ShouldUseParameter(context, leftSymbol, invocation.Expression))
                                        {
                                            var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", parameter.Identifier.ValueText), });
                                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, invocation.Expression.GetLocation(), properties));
                                        }
                                    }

                                    foreach (var memberAccess in walker.MemberAccesses)
                                    {
                                        if (ShouldUseParameter(context, leftSymbol, memberAccess.Expression))
                                        {
                                            var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", parameter.Identifier.ValueText), });
                                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, memberAccess.Expression.GetLocation(), properties));
                                        }
                                    }

                                    foreach (var conditionalAccess in walker.ConditionalAccesses)
                                    {
                                        if (ShouldUseParameter(context, leftSymbol, conditionalAccess.Expression))
                                        {
                                            var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", parameter.Identifier.ValueText), });
                                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, conditionalAccess.Expression.GetLocation(), properties));
                                        }
                                    }

                                    foreach (var binaryExpression in walker.BinaryExpressionSyntaxes)
                                    {
                                        if (ShouldUseParameter(context, leftSymbol, binaryExpression.Left))
                                        {
                                            var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", parameter.Identifier.ValueText), });
                                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, binaryExpression.Left.GetLocation(), properties));
                                        }

                                        if (ShouldUseParameter(context, leftSymbol, binaryExpression.Right))
                                        {
                                            var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", parameter.Identifier.ValueText), });
                                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, binaryExpression.Right.GetLocation(), properties));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (walker.Unassigned.Count > 0)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.GU0004AssignAllReadOnlyMembers,
                                constructorDeclaration.Identifier.GetLocation(),
                                string.Join(Environment.NewLine, walker.Unassigned.Select(x => x.ToDisplayString()))));
                    }
                }
            }
        }

        private static bool ShouldRename(ParameterSyntax parameter, CtorWalker walker, SyntaxNodeAnalysisContext context, out string name)
        {
            name = null;
            if (parameter.Parent is ParameterListSyntax parameterList &&
                parameterList.Parent is ConstructorDeclarationSyntax constructorDeclaration)
            {
                foreach (var assignment in walker.Assignments)
                {
                    if (constructorDeclaration.Contains(assignment) &&
                        assignment.Right is IdentifierNameSyntax right &&
                        right.Identifier.ValueText == parameter.Identifier.ValueText &&
                        TryGetIdentifier(assignment.Left, out var left))
                    {
                        if (IsMatch(left, parameter, out var newName))
                        {
                            return false;
                        }

                        if (name != null &&
                            name != newName)
                        {
                            return false;
                        }

                        name = newName;
                    }
                }

                if (name != null)
                {
                    return true;
                }

                if (constructorDeclaration.Initializer is ConstructorInitializerSyntax initializer &&
                    initializer.ArgumentList is ArgumentListSyntax argumentList &&
                    argumentList.Arguments.TrySingle(syntax => IsParameter(syntax), out var argument) &&
                    context.SemanticModel.GetSymbolSafe(initializer, context.CancellationToken) is IMethodSymbol chained &&
                    chained.TryFindParameter(argument, out var parameterSymbol) &&
                    parameterSymbol.IsParams == parameter.Modifiers.Any(SyntaxKind.ParamKeyword) &&
                    parameterSymbol.Name != parameter.Identifier.ValueText &&
                    !char.IsDigit(parameterSymbol.Name[parameterSymbol.Name.Length - 1]))
                {
                    name = parameterSymbol.Name;
                    return true;
                }
            }

            return false;

            bool IsMatch(IdentifierNameSyntax left, ParameterSyntax right, out string newName)
            {
                newName = null;
                if (Equals(left.Identifier.ValueText, right.Identifier.ValueText))
                {
                    return true;
                }

                newName = left.Identifier.ValueText;
                newName = IsAllCaps(newName)
                    ? newName.ToLowerInvariant()
                    : FirstCharLowercase(newName.TrimStart('_'));

                return false;

                bool Equals(string memberName, string parameterName)
                {
                    if (memberName.StartsWith("_"))
                    {
                        if (parameterName.Length != memberName.Length - 1)
                        {
                            return false;
                        }

                        for (var i = 0; i < parameterName.Length; i++)
                        {
                            if (parameterName[i] != memberName[i + 1])
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    return string.Equals(memberName, parameterName, StringComparison.OrdinalIgnoreCase);
                }

                bool IsAllCaps(string text)
                {
                    foreach (var c in text)
                    {
                        if (char.IsLetter(c) && char.IsLower(c))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                string FirstCharLowercase(string text)
                {
                    if (char.IsLower(text[0]))
                    {
                        return text;
                    }

                    var charArray = text.ToCharArray();
                    charArray[0] = char.ToLower(charArray[0]);
                    return new string(charArray);
                }
            }

            bool IsParameter(ArgumentSyntax argument)
            {
                return argument.Expression is IdentifierNameSyntax identifierName &&
                       identifierName.Identifier.ValueText == parameter.Identifier.ValueText;
            }
        }

        private static bool ShouldUseParameter(SyntaxNodeAnalysisContext context, ISymbol left, ExpressionSyntax expression)
        {
            if (expression.FirstAncestor<AnonymousFunctionExpressionSyntax>() != null)
            {
                if (left is IFieldSymbol field &&
                    !field.IsReadOnly)
                {
                    return false;
                }

                if (left is IPropertySymbol property &&
                    !property.IsGetOnly())
                {
                    return false;
                }
            }

            return TryGetIdentifier(expression, out var identifierName) &&
                   identifierName.IsSymbol(left, context.SemanticModel, context.CancellationToken) &&
                   IsMutatedOnceBefore();

            bool IsMutatedOnceBefore()
            {
                if (expression.TryFirstAncestor(out StatementSyntax statement))
                {
                    using (var walker = MutationWalker.For(left, context.SemanticModel, context.CancellationToken))
                    {
                        return walker.TrySingle(out var single) &&
                               single.TryFirstAncestor(out StatementSyntax singleStatement) &&
                               singleStatement.IsExecutedBefore(statement) == ExecutedBefore.Yes;
                    }
                }

                return false;
            }
        }

        private static bool TryGetIdentifier(ExpressionSyntax expression, out IdentifierNameSyntax result)
        {
            result = expression as IdentifierNameSyntax;
            if (result != null)
            {
                return true;
            }

            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Expression is ThisExpressionSyntax)
                {
                    return TryGetIdentifier(memberAccess.Name, out result);
                }

                if (memberAccess.Expression is IdentifierNameSyntax candidate &&
                    expression.FirstAncestor<TypeDeclarationSyntax>() is TypeDeclarationSyntax typeDeclaration &&
                    candidate.Identifier.ValueText == typeDeclaration.Identifier.ValueText)
                {
                    return TryGetIdentifier(memberAccess.Name, out result);
                }
            }

            return false;
        }

        private class CtorWalker : PooledWalker<CtorWalker>
        {
            private readonly List<ISymbol> unassigned = new List<ISymbol>();
            private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();
            private readonly List<ArgumentSyntax> arguments = new List<ArgumentSyntax>();
            private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();
            private readonly List<MemberAccessExpressionSyntax> memberAccesses = new List<MemberAccessExpressionSyntax>();
            private readonly List<ConditionalAccessExpressionSyntax> conditionalAccesses = new List<ConditionalAccessExpressionSyntax>();
            private readonly List<BinaryExpressionSyntax> binaryExpressionSyntaxes = new List<BinaryExpressionSyntax>();
            private readonly HashSet<SyntaxNode> visited = new HashSet<SyntaxNode>();

            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private CtorWalker()
            {
            }

            internal IReadOnlyList<ISymbol> Unassigned => this.unassigned;

            internal IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

            internal IReadOnlyList<ArgumentSyntax> Arguments => this.arguments;

            internal IReadOnlyList<InvocationExpressionSyntax> Invocations => this.invocations;

            internal IReadOnlyList<MemberAccessExpressionSyntax> MemberAccesses => this.memberAccesses;

            internal IReadOnlyList<ConditionalAccessExpressionSyntax> ConditionalAccesses => this.conditionalAccesses;

            internal IReadOnlyList<BinaryExpressionSyntax> BinaryExpressionSyntaxes => this.binaryExpressionSyntaxes;

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (TryGetIdentifier(node.Left, out var identifierName))
                {
                    this.assignments.Add(node);
                    this.unassigned.Remove(this.semanticModel.GetSymbolSafe(identifierName, this.cancellationToken));
                }

                base.VisitAssignmentExpression(node);
            }

            public override void VisitArgument(ArgumentSyntax node)
            {
                if (TryGetIdentifier(node.Expression, out var identifierName) &&
                    node.RefOrOutKeyword.IsEither(SyntaxKind.RefKeyword, SyntaxKind.OutKeyword))
                {
                    this.unassigned.Remove(this.semanticModel.GetSymbolSafe(identifierName, this.cancellationToken));
                }

                this.arguments.Add(node);
                base.VisitArgument(node);
            }

            public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
            {
                if (this.visited.Add(node) &&
                    this.semanticModel.GetSymbolSafe(node, this.cancellationToken) is IMethodSymbol ctor &&
                    ctor.TrySingleDeclaration(this.cancellationToken, out ConstructorDeclarationSyntax declaration))
                {
                    this.Visit(declaration);
                }

                base.VisitConstructorInitializer(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                this.invocations.Add(node);
                base.VisitInvocationExpression(node);
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                this.memberAccesses.Add(node);
                base.VisitMemberAccessExpression(node);
            }

            public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
            {
                this.conditionalAccesses.Add(node);
                base.VisitConditionalAccessExpression(node);
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                this.binaryExpressionSyntaxes.Add(node);
                base.VisitBinaryExpression(node);
            }

            internal static CtorWalker Borrow(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var walker = Borrow(() => new CtorWalker());
                walker.semanticModel = semanticModel;
                walker.cancellationToken = cancellationToken;
                walker.AddReadOnlies(constructor);
                walker.Visit(constructor);
                return walker;
            }

            protected override void Clear()
            {
                this.unassigned.Clear();
                this.assignments.Clear();
                this.arguments.Clear();
                this.invocations.Clear();
                this.memberAccesses.Clear();
                this.conditionalAccesses.Clear();
                this.binaryExpressionSyntaxes.Clear();
                this.visited.Clear();
                this.semanticModel = null;
                this.cancellationToken = CancellationToken.None;
            }

            private void AddReadOnlies(ConstructorDeclarationSyntax ctor)
            {
                if (ctor.Parent is TypeDeclarationSyntax typeDeclarationSyntax)
                {
                    foreach (var member in typeDeclarationSyntax.Members)
                    {
                        var isStatic = ctor.Modifiers.Any(SyntaxKind.StaticKeyword);
                        if (member is FieldDeclarationSyntax fieldDeclaration &&
                            fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) &&
                            fieldDeclaration.Declaration.Variables.TryLast(out var last) &&
                            last.Initializer == null &&
                            isStatic == fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
                        {
                            foreach (var variable in fieldDeclaration.Declaration.Variables)
                            {
                                this.unassigned.Add(this.semanticModel.GetDeclaredSymbolSafe(variable, this.cancellationToken));
                            }
                        }
                        else if (member is PropertyDeclarationSyntax propertyDeclaration &&
                                 propertyDeclaration.ExpressionBody == null &&
                                 !propertyDeclaration.TryGetSetter(out _) &&
                                 propertyDeclaration.TryGetGetter(out var getter) &&
                                 getter.Body == null &&
                                 propertyDeclaration.Initializer == null &&
                                 !propertyDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
                                 isStatic == propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
                        {
                            this.unassigned.Add(this.semanticModel.GetDeclaredSymbolSafe(propertyDeclaration, this.cancellationToken));
                        }
                    }
                }
            }
        }
    }
}
