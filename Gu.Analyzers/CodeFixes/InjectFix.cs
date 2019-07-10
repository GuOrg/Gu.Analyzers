namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InjectFix))]
    [Shared]
    internal class InjectFix : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0007PreferInjecting.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ExpressionSyntax node))
                {
                    if (node is ObjectCreationExpressionSyntax objectCreation &&
                        semanticModel.TryGetType(objectCreation, context.CancellationToken, out var createdType))
                    {
                        var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName(createdType)))
                                                           .WithType(objectCreation.Type);
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Inject",
                                cancellationToken => ApplyFixAsync(context, semanticModel, cancellationToken, objectCreation, parameterSyntax),
                                nameof(InjectFix)),
                            diagnostic);
                    }
                    else if (node is IdentifierNameSyntax identifierName &&
                             identifierName.Parent is MemberAccessExpressionSyntax memberAccess)
                    {
                        var type = GU0007PreferInjecting.MemberType(memberAccess, semanticModel, context.CancellationToken);
                        var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName(type)))
                                                           .WithType(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Inject UNSAFE",
                                cancellationToken => ApplyFixAsync(context, semanticModel, cancellationToken, identifierName, parameterSyntax),
                                nameof(InjectFix)),
                            diagnostic);
                    }
                }
            }
        }

        private static async Task<Document> ApplyFixAsync(CodeFixContext context, SemanticModel semanticModel, CancellationToken cancellationToken, ExpressionSyntax expression, ParameterSyntax parameterSyntax)
        {
            if (Inject.TryFindConstructor(expression, out var ctor))
            {
                var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                                 .ConfigureAwait(false);
                parameterSyntax = UniqueName(ctor.ParameterList, parameterSyntax);
                if (expression is ObjectCreationExpressionSyntax)
                {
                    editor.ReplaceNode(expression, SyntaxFactory.IdentifierName(parameterSyntax.Identifier));
                }
                else if (expression is IdentifierNameSyntax identifierName &&
                         expression.Parent is MemberAccessExpressionSyntax)
                {
                    var replaceNodes = new ReplaceNodes(identifierName, semanticModel, cancellationToken).Nodes;
                    if (replaceNodes.Count == 0)
                    {
                        return context.Document;
                    }

                    ExpressionSyntax fieldAccess = null;
                    foreach (var replaceNode in replaceNodes)
                    {
                        if (replaceNode.FirstAncestor<ConstructorDeclarationSyntax>() == null)
                        {
                            if (fieldAccess == null)
                            {
                                fieldAccess = WithField(editor, ctor, parameterSyntax);
                            }

                            editor.ReplaceNode(replaceNode, fieldAccess.WithLeadingTrivia(replaceNode.GetLeadingTrivia()));
                        }
                        else
                        {
                            editor.ReplaceNode(replaceNode, SyntaxFactory.IdentifierName(parameterSyntax.Identifier).WithLeadingTrivia(replaceNode.GetLeadingTrivia()));
                        }
                    }
                }
                else
                {
                    return context.Document;
                }

                if (ctor.ParameterList == null)
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

                return editor.GetChangedDocument();
            }

            return context.Document;
        }

        private static ParameterSyntax UniqueName(ParameterListSyntax parameterList, ParameterSyntax parameter)
        {
            if (parameterList != null)
            {
                foreach (var p in parameterList.Parameters)
                {
                    if (p.Identifier.ValueText == parameter.Identifier.ValueText)
                    {
                        return UniqueName(
                            parameterList,
                            parameter.WithIdentifier(SyntaxFactory.Identifier(parameter.Identifier.ValueText + "_")));
                    }
                }
            }

            return parameter;
        }

        private static string ParameterName(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType &&
                namedType.IsGenericType)
            {
                var definition = namedType.OriginalDefinition;
                if (definition?.TypeParameters.Length == 1)
                {
                    var parameter = definition.TypeParameters[0];
                    foreach (var constraintType in parameter.ConstraintTypes)
                    {
                        if (type.Name.Contains(constraintType.Name))
                        {
                            return type.Name.Replace(constraintType.Name, namedType.TypeArguments[0].Name)
                                       .ToFirstCharLower();
                        }
                    }
                }
            }

            return type.Name.ToFirstCharLower();
        }

        private static ExpressionSyntax WithField(DocumentEditor editor, ConstructorDeclarationSyntax ctor, ParameterSyntax parameter)
        {
            var underscoreFields = editor.SemanticModel.UnderscoreFields();
            var name = underscoreFields
                           ? "_" + parameter.Identifier.ValueText
                           : parameter.Identifier.ValueText;
            var containingType = ctor.FirstAncestor<TypeDeclarationSyntax>();
            var declaredSymbol = editor.SemanticModel.GetDeclaredSymbol(containingType);
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
                editor.AddMember(containingType, newField);
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
                this.Visit(identifierName.FirstAncestor<ClassDeclarationSyntax>());
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

            private bool TryGetReplaceNode(IdentifierNameSyntax node, IdentifierNameSyntax expected, out SyntaxNode result)
            {
                result = null;
                if (node.Identifier.ValueText != expected.Identifier.ValueText)
                {
                    return false;
                }

                var member = node.FirstAncestor<MemberDeclarationSyntax>();
                if (member == null ||
                    this.semanticModel.GetDeclaredSymbolSafe(member, this.cancellationToken)?.IsStatic != false)
                {
                    return false;
                }

                return this.TryGetReplaceNode(node.Parent as MemberAccessExpressionSyntax, expected.Parent as MemberAccessExpressionSyntax, out result);
            }

            private bool TryGetReplaceNode(MemberAccessExpressionSyntax node, MemberAccessExpressionSyntax expected, out SyntaxNode result)
            {
                result = null;
                if (node != null &&
                    expected != null &&
                    node.Name.Identifier.ValueText == expected.Name.Identifier.ValueText &&
                    this.semanticModel.TryGetSymbol(node, this.cancellationToken, out ISymbol nodeSymbol) &&
                    this.semanticModel.TryGetSymbol(this.identifierName, this.cancellationToken, out ISymbol expectedSymbol) &&
                    nodeSymbol.Equals(expectedSymbol) &&
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
