namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InjectCodeFixProvider))]
    [Shared]
    internal class InjectCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GU0007PreferInjecting.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) || token.IsMissing)
                {
                    continue;
                }

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (node is ObjectCreationExpressionSyntax objectCreation)
                {
                    var type = (ITypeSymbol)semanticModel.GetSymbolSafe(objectCreation, context.CancellationToken)?.ContainingSymbol;
                    if (type == null)
                    {
                        continue;
                    }

                    var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName(type)))
                                                       .WithType(objectCreation.Type);
                    switch (GU0007PreferInjecting.CanInject(objectCreation, semanticModel, context.CancellationToken))
                    {
                        case GU0007PreferInjecting.Injectable.No:
                            continue;
                        case GU0007PreferInjecting.Injectable.Safe:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Inject",
                                    cancellationToken => ApplyFixAsync(context, semanticModel, cancellationToken, objectCreation, parameterSyntax),
                                    nameof(InjectCodeFixProvider)),
                                diagnostic);
                            break;
                        case GU0007PreferInjecting.Injectable.Unsafe:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Inject UNSAFE",
                                    cancellationToken => ApplyFixAsync(context, semanticModel, cancellationToken, objectCreation, parameterSyntax),
                                    nameof(InjectCodeFixProvider)),
                                diagnostic);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (node is IdentifierNameSyntax identifierName &&
                    identifierName.Parent is MemberAccessExpressionSyntax memberAccess)
                {
                    var type = GU0007PreferInjecting.MemberType(memberAccess, semanticModel, context.CancellationToken);
                    var parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName(type)))
                                                       .WithType(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                    switch (GU0007PreferInjecting.IsInjectable(identifierName, semanticModel, context.CancellationToken))
                    {
                        case GU0007PreferInjecting.Injectable.No:
                            continue;
                        case GU0007PreferInjecting.Injectable.Safe:
                        case GU0007PreferInjecting.Injectable.Unsafe:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Inject UNSAFE",
                                    cancellationToken => ApplyFixAsync(context, semanticModel, cancellationToken, identifierName, parameterSyntax),
                                    nameof(InjectCodeFixProvider)),
                                diagnostic);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private static async Task<Document> ApplyFixAsync(CodeFixContext context, SemanticModel semanticModel, CancellationToken cancellationToken, ExpressionSyntax expression, ParameterSyntax parameterSyntax)
        {
            if (GU0007PreferInjecting.TryGetSingleConstructor(expression, out ConstructorDeclarationSyntax ctor))
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
                    if (ctor.ParameterList.TryGetFirst(p => p.Default != null || p.Modifiers.Any(SyntaxKind.ParamsKeyword), out ParameterSyntax existing))
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
                                       .FirstCharLower();
                        }
                    }
                }
            }

            return type.Name.FirstCharLower();
        }

        private static ExpressionSyntax WithField(DocumentEditor editor, ConstructorDeclarationSyntax ctor, ParameterSyntax parameter)
        {
            var usesUnderscoreNames = editor.SemanticModel.SyntaxTree.GetRoot().UsesUnderscoreNames(editor.SemanticModel, CancellationToken.None);
            var name = usesUnderscoreNames
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
            if (members.TryGetFirst(x => x is FieldDeclarationSyntax, out MemberDeclarationSyntax field))
            {
                editor.InsertBefore(field, new[] { newField });
            }
            else if (members.TryGetFirst(out field))
            {
                editor.InsertBefore(field, new[] { newField });
            }
            else
            {
                editor.AddMember(containingType, newField);
            }

            var fieldAccess = usesUnderscoreNames
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
            internal readonly List<SyntaxNode> Nodes = new List<SyntaxNode>();

            private readonly IdentifierNameSyntax identifierName;
            private readonly SemanticModel semanticModel;
            private readonly CancellationToken cancellationToken;

            public ReplaceNodes(
                IdentifierNameSyntax identifierName,
                SemanticModel semanticModel,
                CancellationToken cancellationToken)
            {
                this.identifierName = identifierName;
                this.semanticModel = semanticModel;
                this.cancellationToken = cancellationToken;
                this.Visit(identifierName.FirstAncestor<ClassDeclarationSyntax>());
            }

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
                if (this.TryGetReplaceNode(node, this.identifierName, out SyntaxNode replaceNode))
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
                if (node == null || expected == null)
                {
                    return false;
                }

                if (node.Name.Identifier.ValueText != expected.Name.Identifier.ValueText)
                {
                    return false;
                }

                var nodeSymbol = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                var expectedSymbol = this.semanticModel.GetSymbolSafe(this.identifierName, this.cancellationToken);
                if (nodeSymbol == null ||
                    expectedSymbol == null ||
                    !SymbolComparer.Equals(nodeSymbol, expectedSymbol))
                {
                    return false;
                }

                if (!GU0007PreferInjecting.IsRootValid(node, this.semanticModel, this.cancellationToken))
                {
                    return false;
                }

                result = node;
                return true;
            }
        }
    }
}