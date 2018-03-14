namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ConstructorAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GU0003CtorParameterNamesShouldMatch.Descriptor,
            GU0004AssignAllReadOnlyMembers.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ConstructorDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ConstructorDeclarationSyntax constructorDeclaration)
            {
                using (var pooled = CtorWalker.Borrow(constructorDeclaration, context.SemanticModel, context.CancellationToken))
                {
                    if (constructorDeclaration.ParameterList is ParameterListSyntax parameterList &&
                        parameterList.Parameters.Count > 0)
                    {
                        foreach (var assignment in pooled.Assignments)
                        {
                            if (constructorDeclaration.Contains(assignment) &&
                               TryGetIdentifier(assignment.Left, out var left) &&
                               TryGetIdentifier(assignment.Right, out var right) &&
                               parameterList.Parameters.TryFirst(x => x.Identifier.ValueText == right.Identifier.ValueText, out var parameter) &&
                               !parameter.Modifiers.Any(SyntaxKind.ParamsKeyword) &&
                               !IsMatch(left, right, out var name) &&
                                pooled.Assignments.TrySingle(x => x.Right is IdentifierNameSyntax id && id.Identifier.ValueText == parameter.Identifier.ValueText, out _))
                            {
                                var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", name), });
                                context.ReportDiagnostic(Diagnostic.Create(GU0003CtorParameterNamesShouldMatch.Descriptor, parameter.Identifier.GetLocation(), properties));
                            }
                        }

                        if (constructorDeclaration.Initializer is ConstructorInitializerSyntax initializer &&
                            initializer.ArgumentList is ArgumentListSyntax argumentList &&
                            argumentList.Arguments.TryFirst(x => x.Expression is IdentifierNameSyntax, out _))
                        {
                            var chained = context.SemanticModel.GetSymbolSafe(initializer, context.CancellationToken);
                            foreach (var arg in argumentList.Arguments)
                            {
                                if (TryGetIdentifier(arg.Expression, out var identifier) &&
                                    parameterList.Parameters.TryFirst(x => x.Identifier.ValueText == identifier.Identifier.ValueText, out var parameter) &&
                                    chained.TryGetMatchingParameter(arg, out var parameterSymbol) &&
                                    !parameterSymbol.IsParams &&
                                    parameterSymbol.Name != parameter.Identifier.ValueText)
                                {
                                    var properties = ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", parameterSymbol.Name), });
                                    context.ReportDiagnostic(Diagnostic.Create(GU0003CtorParameterNamesShouldMatch.Descriptor, parameter.Identifier.GetLocation(), properties));
                                }
                            }
                        }
                    }

                    if (pooled.Unassigned.Count > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GU0004AssignAllReadOnlyMembers.Descriptor, constructorDeclaration.Identifier.GetLocation(), string.Join(Environment.NewLine, pooled.Unassigned)));
                    }
                }
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

        private static bool IsMatch(IdentifierNameSyntax left, IdentifierNameSyntax right, out string name)
        {
            name = null;
            if (Equals(left.Identifier.ValueText, right.Identifier.ValueText))
            {
                return true;
            }

            name = left.Identifier.ValueText;
            name = IsAllCaps(name)
                ? name.ToLowerInvariant()
                : FirstCharLowercase(name.TrimStart('_'));

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

        private class CtorWalker : PooledWalker<CtorWalker>
        {
            private readonly List<ISymbol> unassigned = new List<ISymbol>();
            private readonly List<AssignmentExpressionSyntax> assignments = new List<AssignmentExpressionSyntax>();
            private readonly HashSet<SyntaxNode> visited = new HashSet<SyntaxNode>();

            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private CtorWalker()
            {
            }

            public IReadOnlyList<ISymbol> Unassigned => this.unassigned;

            public IReadOnlyList<AssignmentExpressionSyntax> Assignments => this.assignments;

            public static CtorWalker Borrow(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var walker = Borrow(() => new CtorWalker());
                walker.semanticModel = semanticModel;
                walker.cancellationToken = cancellationToken;
                walker.AddReadOnlies(constructor);
                walker.Visit(constructor);
                return walker;
            }

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

            protected override void Clear()
            {
                this.unassigned.Clear();
                this.assignments.Clear();
                this.visited.Clear();
                this.semanticModel = null;
                this.cancellationToken = CancellationToken.None;
            }

            private void AddReadOnlies(ConstructorDeclarationSyntax ctor)
            {
                var typeDeclarationSyntax = (TypeDeclarationSyntax)ctor.Parent;
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
