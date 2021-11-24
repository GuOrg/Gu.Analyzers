namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0003CtorParameterNamesShouldMatch,
            Descriptors.GU0004AssignAllReadOnlyMembers,
            Descriptors.GU0014PreferParameter);

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
                using var walker = CtorWalker.Borrow(constructorDeclaration, context.SemanticModel, context.CancellationToken);
                if (constructorDeclaration.ParameterList is { Parameters: { } parameters } &&
                    parameters.Count > 0)
                {
                    foreach (var parameter in parameters)
                    {
                        if (ShouldRename(parameter, walker, context, out var name))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.GU0003CtorParameterNamesShouldMatch,
                                    parameter.Identifier.GetLocation(),
                                    ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string?>("Name", name) })));
                        }
                    }

                    foreach (var assignment in walker.Assignments)
                    {
                        if (constructorDeclaration.Contains(assignment) &&
                            TryGetIdentifier(assignment.Left, out var left))
                        {
                            if (TryGetIdentifier(assignment.Right, out var right) &&
                                parameters.TryFirst(x => x.Identifier.ValueText == right.Identifier.ValueText, out var parameter) &&
                                !parameter.Modifiers.Any(SyntaxKind.ParamsKeyword) &&
                                walker.Assignments.TrySingle(x => x.Right is IdentifierNameSyntax id && id.Identifier.ValueText == parameter.Identifier.ValueText, out _) &&
                                context.SemanticModel.TryGetSymbol(left, context.CancellationToken, out var leftSymbol))
                            {
                                foreach (var argument in walker.Arguments)
                                {
                                    if (argument.Parent is ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } &&
                                        invocation.TryGetMethodName(out var methodName) &&
                                        methodName == "nameof")
                                    {
                                        continue;
                                    }

                                    if (ShouldUseParameter(context, leftSymbol, argument.Expression))
                                    {
                                        var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string?>("Name", parameter.Identifier.ValueText), });
                                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, argument.Expression.GetLocation(), properties));
                                    }
                                }

                                foreach (var invocation in walker.Invocations)
                                {
                                    if (ShouldUseParameter(context, leftSymbol, invocation.Expression))
                                    {
                                        var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string?>("Name", parameter.Identifier.ValueText), });
                                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, invocation.Expression.GetLocation(), properties));
                                    }
                                }

                                foreach (var memberAccess in walker.MemberAccesses)
                                {
                                    if (ShouldUseParameter(context, leftSymbol, memberAccess.Expression))
                                    {
                                        var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string?>("Name", parameter.Identifier.ValueText), });
                                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, memberAccess.Expression.GetLocation(), properties));
                                    }
                                }

                                foreach (var conditionalAccess in walker.ConditionalAccesses)
                                {
                                    if (ShouldUseParameter(context, leftSymbol, conditionalAccess.Expression))
                                    {
                                        var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string?>("Name", parameter.Identifier.ValueText), });
                                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, conditionalAccess.Expression.GetLocation(), properties));
                                    }
                                }

                                foreach (var binaryExpression in walker.BinaryExpressionSyntaxes)
                                {
                                    if (ShouldUseParameter(context, leftSymbol, binaryExpression.Left))
                                    {
                                        var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string?>("Name", parameter.Identifier.ValueText), });
                                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0014PreferParameter, binaryExpression.Left.GetLocation(), properties));
                                    }

                                    if (ShouldUseParameter(context, leftSymbol, binaryExpression.Right))
                                    {
                                        var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string?>("Name", parameter.Identifier.ValueText), });
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

        private static bool ShouldRename(ParameterSyntax parameter, CtorWalker walker, SyntaxNodeAnalysisContext context, [NotNullWhen(true)] out string? name)
        {
            name = null;
            if (parameter.Parent is ParameterListSyntax { Parent: ConstructorDeclarationSyntax ctor })
            {
                foreach (var assignment in walker.Assignments)
                {
                    if (ctor.Contains(assignment) &&
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

                if (ctor.Initializer is { ArgumentList: { Arguments: { } arguments } } initializer &&
                    arguments.TrySingle(syntax => IsParameter(syntax), out var argument) &&
                    context.SemanticModel.GetSymbolSafe(initializer, context.CancellationToken) is { } chained &&
                    chained.TryFindParameter(argument, out var parameterSymbol) &&
                    parameterSymbol.IsParams == parameter.Modifiers.Any(SyntaxKind.ParamKeyword) &&
                    parameterSymbol.Name != parameter.Identifier.ValueText &&
                    !char.IsDigit(parameterSymbol.Name.Last()))
                {
                    name = parameterSymbol.Name;
                    return true;
                }
            }

            return false;

            static bool IsMatch(IdentifierNameSyntax left, ParameterSyntax right, out string? newName)
            {
                newName = null;
                if (IsMatch(left.Identifier.ValueText, right.Identifier.ValueText))
                {
                    return true;
                }

                newName = left.Identifier.ValueText;
                newName = IsAllCaps(newName)
                    ? newName.ToLowerInvariant()
                    : FirstCharLowercase(newName.TrimStart('_'));

                return false;

                static bool IsMatch(string memberName, string parameterName)
                {
                    if (memberName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
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

                static bool IsAllCaps(string text)
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

                static string FirstCharLowercase(string text)
                {
                    if (char.IsLower(text[0]))
                    {
                        return text;
                    }

                    var charArray = text.ToCharArray();
                    charArray[0] = char.ToLower(charArray[0], CultureInfo.InvariantCulture);
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
                if (expression.TryFirstAncestor(out StatementSyntax? statement))
                {
                    using var walker = MutationWalker.For(left, context.SemanticModel, context.CancellationToken);
                    return walker.TrySingle(out var single) &&
                           single.TryFirstAncestor(out StatementSyntax? singleStatement) &&
                           singleStatement.IsExecutedBefore(statement) == ExecutedBefore.Yes;
                }

                return false;
            }
        }

        private static bool TryGetIdentifier(ExpressionSyntax expression, [NotNullWhen(true)] out IdentifierNameSyntax? result)
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
                    expression.FirstAncestor<TypeDeclarationSyntax>() is { } typeDeclaration &&
                    candidate.Identifier.ValueText == typeDeclaration.Identifier.ValueText)
                {
                    return TryGetIdentifier(memberAccess.Name, out result);
                }
            }

            return false;
        }

        private class CtorWalker : PooledWalker<CtorWalker>
        {
            private readonly List<ISymbol> unassigned = new();
            private readonly List<AssignmentExpressionSyntax> assignments = new();
            private readonly List<ArgumentSyntax> arguments = new();
            private readonly List<InvocationExpressionSyntax> invocations = new();
            private readonly List<MemberAccessExpressionSyntax> memberAccesses = new();
            private readonly List<ConditionalAccessExpressionSyntax> conditionalAccesses = new();
            private readonly List<BinaryExpressionSyntax> binaryExpressionSyntaxes = new();
            private readonly HashSet<SyntaxNode> visited = new();

            private SemanticModel semanticModel = null!;
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
                if (TryGetIdentifier(node.Left, out var identifierName) &&
                    this.semanticModel.TryGetSymbol(identifierName, this.cancellationToken, out var symbol))
                {
                    this.assignments.Add(node);
                    this.unassigned.Remove(symbol);
                }

                base.VisitAssignmentExpression(node);
            }

            public override void VisitArgument(ArgumentSyntax node)
            {
                if (TryGetIdentifier(node.Expression, out var identifierName) &&
                    node.RefOrOutKeyword.IsEither(SyntaxKind.RefKeyword, SyntaxKind.OutKeyword) &&
                    this.semanticModel.TryGetSymbol(identifierName, this.cancellationToken, out var symbol))
                {
                    this.unassigned.Remove(symbol);
                }

                this.arguments.Add(node);
                base.VisitArgument(node);
            }

            public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
            {
                if (this.visited.Add(node) &&
                    this.semanticModel.GetSymbolSafe(node, this.cancellationToken) is { } ctor &&
                    ctor.TrySingleDeclaration(this.cancellationToken, out ConstructorDeclarationSyntax? declaration))
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
                this.semanticModel = null!;
                this.cancellationToken = CancellationToken.None;
            }

            private void AddReadOnlies(ConstructorDeclarationSyntax ctor)
            {
                if (ctor.Parent is TypeDeclarationSyntax typeDeclarationSyntax)
                {
                    foreach (var member in typeDeclarationSyntax.Members)
                    {
                        var isStatic = ctor.Modifiers.Any(SyntaxKind.StaticKeyword);
                        switch (member)
                        {
                            case FieldDeclarationSyntax { Modifiers: { } modifiers, Declaration: { Variables: { } variables } }
                                when modifiers.Any(SyntaxKind.ReadOnlyKeyword) &&
                                     isStatic == modifiers.Any(SyntaxKind.StaticKeyword) &&
                                     variables.LastOrDefault() is { Initializer: null }:
                                foreach (var variable in variables)
                                {
                                    if (this.semanticModel.GetDeclaredSymbol(variable, this.cancellationToken) is { } symbol)
                                    {
                                        this.unassigned.Add(symbol);
                                    }
                                }

                                break;
                            case PropertyDeclarationSyntax { Modifiers: { } modifiers, ExpressionBody: null, AccessorList: { Accessors: { Count: 1 } accessors }, Initializer: null } property
                                when accessors[0] is { Keyword: { ValueText: "get" }, Body: null } &&
                                     !modifiers.Any(SyntaxKind.AbstractKeyword) &&
                                     isStatic == modifiers.Any(SyntaxKind.StaticKeyword) &&
                                     this.semanticModel.GetDeclaredSymbol(property, this.cancellationToken) is { } type:
                                this.unassigned.Add(type);
                                break;
                        }
                    }
                }
            }
        }
    }
}
