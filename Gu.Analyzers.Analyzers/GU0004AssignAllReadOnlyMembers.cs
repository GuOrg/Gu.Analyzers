namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0004AssignAllReadOnlyMembers : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0004";
        private const string Title = "Assign all readonly members.";
        private const string MessageFormat = "Assign all readonly members.";
        private const string Description = "Assign all readonly members.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.Correctness,
            DiagnosticSeverity.Hidden,
            AnalyzerConstants.EnabledByDefault,
            Description,
            HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleConstructor, SyntaxKind.ConstructorDeclaration);
        }

        private static void HandleConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructorDeclarationSyntax = (ConstructorDeclarationSyntax)context.Node;

            using (var walker = CtorWalker.Create(constructorDeclarationSyntax, context.SemanticModel, context.CancellationToken))
            {
                if (walker.Unassigned.Any())
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, constructorDeclarationSyntax.GetLocation()));
                }
            }
        }

        private class CtorWalker : CSharpSyntaxWalker, IDisposable
        {
            private static readonly ConcurrentQueue<CtorWalker> Cache = new ConcurrentQueue<CtorWalker>();

            private readonly List<string> readOnlies = new List<string>();

            private CtorWalker()
            {
            }

            public IReadOnlyList<string> Unassigned => this.readOnlies;

            public static CtorWalker Create(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                CtorWalker walker;
                if (!Cache.TryDequeue(out walker))
                {
                    walker = new CtorWalker();
                }

                walker.readOnlies.AddRange(ReadOnlies(constructor, semanticModel, cancellationToken));
                walker.Visit(constructor);
                return walker;
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                IdentifierNameSyntax left;
                if (TryGetIdentifier(node.Left, out left))
                {
                    this.readOnlies.Remove(left.Identifier.ValueText);
                }

                base.VisitAssignmentExpression(node);
            }

            private static IEnumerable<string> ReadOnlies(ConstructorDeclarationSyntax ctor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var isStatic = semanticModel.GetDeclaredSymbol(ctor, cancellationToken).IsStatic;
                var classDeclarationSyntax = (ClassDeclarationSyntax)ctor.Parent;
                foreach (var member in classDeclarationSyntax.Members)
                {
                    var fieldDeclarationSyntax = member as FieldDeclarationSyntax;
                    if (fieldDeclarationSyntax != null)
                    {
                        var declaration = fieldDeclarationSyntax.Declaration;
                        VariableDeclaratorSyntax variable;
                        if (declaration.Variables.TryGetSingle(out variable))
                        {
                            var symbol = (IFieldSymbol)semanticModel.GetDeclaredSymbol(variable, cancellationToken);
                            if (symbol.IsReadOnly && symbol.IsStatic == isStatic && variable.Initializer == null)
                            {
                                yield return fieldDeclarationSyntax.Identifier().ValueText;
                            }
                        }

                        continue;
                    }

                    var propertyDeclarationSyntax = member as PropertyDeclarationSyntax;
                    if (propertyDeclarationSyntax != null)
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax, cancellationToken);
                        if (symbol.IsReadOnly && symbol.IsStatic == isStatic && propertyDeclarationSyntax.Initializer == null)
                        {
                            yield return propertyDeclarationSyntax.Identifier().ValueText;
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

                var member = expression as MemberAccessExpressionSyntax;
                if (member?.Expression is ThisExpressionSyntax)
                {
                    return TryGetIdentifier(member.Name, out result);
                }

                return false;
            }

            public void Dispose()
            {
                this.readOnlies.Clear();
                Cache.Enqueue(this);
            }
        }
    }
}