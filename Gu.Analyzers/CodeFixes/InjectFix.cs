namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InjectFix))]
    [Shared]
    internal class InjectFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Descriptors.GU0007PreferInjecting.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot is { } &&
                    syntaxRoot.TryFindNode(diagnostic, out ExpressionSyntax? node) &&
                    diagnostic.Properties.TryGetValue(nameof(INamedTypeSymbol), out var typeName) &&
                    diagnostic.Properties.TryGetValue(nameof(Inject.Injectable), out var injectable))
                {
                    context.RegisterCodeFix(
                        $"Inject {(injectable == nameof(Inject.Injectable.Safe) ? injectable.ToLowerInvariant() : injectable.ToUpperInvariant())}.",
                        (editor, cancellationToken) => Fix(editor, node, typeName, cancellationToken),
                        nameof(InjectFix),
                        diagnostic);
                }
            }
        }

        private static void Fix(DocumentEditor editor, ExpressionSyntax expression, string typeName, CancellationToken cancellationToken)
        {
            if (Inject.FindConstructor(expression) is { } ctor)
            {
                var type = SyntaxFactory.ParseTypeName(typeName);
                var parameterSyntax = SyntaxFactory.Parameter(
                    attributeLists: default,
                    modifiers: default,
                    type: type,
                    identifier: SyntaxFactory.Identifier(ParameterName(ctor.ParameterList, expression, typeName)),
                    @default: default);
                if (expression is ObjectCreationExpressionSyntax)
                {
                    editor.ReplaceNode(expression, SyntaxFactory.IdentifierName(parameterSyntax.Identifier));
                }
                else if (expression is IdentifierNameSyntax identifierName &&
                         expression.Parent is MemberAccessExpressionSyntax)
                {
                    var replaceNodes = new ReplaceNodes(identifierName, editor.SemanticModel, cancellationToken).Nodes;
                    if (replaceNodes.Count == 0)
                    {
                        return;
                    }

                    ExpressionSyntax? fieldAccess = null;
                    foreach (var replaceNode in replaceNodes)
                    {
                        if (replaceNode.FirstAncestor<ConstructorDeclarationSyntax>() is null)
                        {
                            fieldAccess ??= WithField(editor, ctor, parameterSyntax);
                            if (fieldAccess is { })
                            {
                                editor.ReplaceNode(replaceNode, fieldAccess.WithLeadingTrivia(replaceNode.GetLeadingTrivia()));
                            }
                        }
                        else
                        {
                            editor.ReplaceNode(replaceNode, SyntaxFactory.IdentifierName(parameterSyntax.Identifier).WithLeadingTrivia(replaceNode.GetLeadingTrivia()));
                        }
                    }
                }
                else
                {
                    return;
                }

                if (ctor.ParameterList is null)
                {
                    editor.ReplaceNode(ctor, ctor.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameterSyntax))));
                }
                else
                {
                    if (ctor.ParameterList.Parameters.TryFirst(p => p.Default != null || p.Modifiers.Any(SyntaxKind.ParamsKeyword), out var existing))
                    {
                        editor.InsertBefore(existing, parameterSyntax);
                    }
                    else
                    {
                        editor.ReplaceNode(ctor.ParameterList, ctor.ParameterList.AddParameters(parameterSyntax));
                    }
                }
            }
        }

        private static string ParameterName(ParameterListSyntax parameterList, ExpressionSyntax expression, string typeName)
        {
            while (expression.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                expression = memberAccess;
            }

            if (expression.Parent is AssignmentExpressionSyntax assignment &&
                assignment.Right.Contains(expression))
            {
                return assignment.Left switch
                {
                    MemberAccessExpressionSyntax memberAccess => UniqueName(memberAccess.Name.Identifier.ValueText.TrimStart('_').ToFirstCharLower()),
                    IdentifierNameSyntax identifierName => UniqueName(identifierName.Identifier.ValueText.TrimStart('_').ToFirstCharLower()),
                    _ => throw new NotSupportedException("Could not figure out parameter name from assignment."),
                };
            }

            var index = typeName.IndexOf('<');
            if (index > 0)
            {
                return ParameterName(parameterList, expression, typeName.Substring(0, index));
            }

            return UniqueName(typeName.ToFirstCharLower());

            string UniqueName(string candidate)
            {
                if (parameterList != null)
                {
                    foreach (var p in parameterList.Parameters)
                    {
                        if (p.Identifier.ValueText == candidate)
                        {
                            return UniqueName(candidate + "_");
                        }
                    }
                }

                return candidate;
            }
        }

        private static ExpressionSyntax? WithField(DocumentEditor editor, ConstructorDeclarationSyntax ctor, ParameterSyntax parameter)
        {
            if (ctor is { Body: { }, Parent: TypeDeclarationSyntax containingType } &&
                editor.SemanticModel.GetDeclaredSymbol(containingType) is { } declaredSymbol)
            {
                var underscoreFields = editor.SemanticModel.UnderscoreFields() == CodeStyleResult.Yes;
                var name = underscoreFields
                               ? "_" + parameter.Identifier.ValueText
                               : parameter.Identifier.ValueText;
                while (declaredSymbol.MemberNames.Contains(name))
                {
                    name += "_";
                }

                var newField = (FieldDeclarationSyntax)editor.Generator.FieldDeclaration(
                      name,
                      accessibility: Accessibility.Private,
                      modifiers: DeclarationModifiers.ReadOnly,
                      type: parameter.Type);
                var members = containingType.Members;
                if (members.TryFirst(x => x is FieldDeclarationSyntax, out var field))
                {
                    editor.InsertBefore(field, new[] { newField });
                }
                else if (members.TryFirst(out field))
                {
                    editor.InsertBefore(field, new[] { newField });
                }
                else
                {
                    _ = editor.AddMember(containingType, newField);
                }

                var fieldAccess = underscoreFields
                                           ? SyntaxFactory.IdentifierName(name)
                                           : SyntaxFactory.ParseExpression($"this.{name}");

                var assignStatement = SyntaxFactory.ExpressionStatement(
                                                                 (ExpressionSyntax)editor.Generator.AssignmentStatement(
                                                                     fieldAccess,
                                                                     SyntaxFactory.IdentifierName(parameter.Identifier)))
                                                             .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                                             .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
                if (ctor.Body.Statements.Any())
                {
                    editor.InsertBefore(
                        ctor.Body.Statements.First(),
                        assignStatement);
                }
                else
                {
                    editor.ReplaceNode(ctor.Body, ctor.Body.WithStatements(ctor.Body.Statements.Add(assignStatement)));
                }

                return fieldAccess;
            }

            return null;
        }

        private class ReplaceNodes : CSharpSyntaxWalker
        {
            private readonly IdentifierNameSyntax identifierName;
            private readonly SemanticModel semanticModel;
            private readonly CancellationToken cancellationToken;

            internal ReplaceNodes(
                IdentifierNameSyntax identifierName,
                SemanticModel semanticModel,
                CancellationToken cancellationToken)
            {
                this.identifierName = identifierName;
                this.semanticModel = semanticModel;
                this.cancellationToken = cancellationToken;
                this.Visit(identifierName.FirstAncestor<ClassDeclarationSyntax>() ?? throw new InvalidOperationException("Did not find a class declaration."));
            }

            internal List<SyntaxNode> Nodes { get; } = new List<SyntaxNode>();

            public sealed override void Visit(SyntaxNode node)
            {
                base.Visit(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (this.identifierName.FirstAncestor<ClassDeclarationSyntax>() != node)
                {
                    return;
                }

                base.VisitClassDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                // NOP, we don't visit recursive types.
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (this.TryGetReplaceNode(node, this.identifierName, out var replaceNode))
                {
                    this.Nodes.Add(replaceNode);
                }

                base.VisitIdentifierName(node);
            }

            private bool TryGetReplaceNode(IdentifierNameSyntax node, IdentifierNameSyntax expected, [NotNullWhen(true)] out SyntaxNode? result)
            {
                result = null;
                if (node.Identifier.ValueText != expected.Identifier.ValueText)
                {
                    return false;
                }

                var member = node.FirstAncestor<MemberDeclarationSyntax>();
                if (member is null ||
                    this.semanticModel.GetDeclaredSymbolSafe(member, this.cancellationToken)?.IsStatic != false)
                {
                    return false;
                }

                return this.TryGetReplaceNode(node.Parent as MemberAccessExpressionSyntax, expected.Parent as MemberAccessExpressionSyntax, out result);
            }

            private bool TryGetReplaceNode(MemberAccessExpressionSyntax? node, MemberAccessExpressionSyntax? expected, [NotNullWhen(true)] out SyntaxNode? result)
            {
                result = null;
                if (node != null &&
                    expected != null &&
                    node.Name.Identifier.ValueText == expected.Name.Identifier.ValueText &&
                    this.semanticModel.TryGetSymbol(node, this.cancellationToken, out var nodeSymbol) &&
                    this.semanticModel.TryGetSymbol(this.identifierName, this.cancellationToken, out var expectedSymbol) &&
                    SymbolComparer.Equal(nodeSymbol, expectedSymbol) &&
                    GU0007PreferInjecting.IsRootValid(node, this.semanticModel, this.cancellationToken))
                {
                    result = node;
                    return true;
                }

                return false;
            }
        }
    }
}
