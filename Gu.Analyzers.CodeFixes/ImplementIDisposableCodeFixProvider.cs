namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIDisposableCodeFixProvider))]
    [Shared]
    internal class ImplementIDisposableCodeFixProvider : CodeFixProvider
    {
        // ReSharper disable once InconsistentNaming
        private static readonly TypeSyntax IDisposableInterface = SyntaxFactory.ParseTypeName("IDisposable");
        private static readonly UsingDirectiveSyntax UsingSystem = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System"));

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            GU0031DisposeMember.DiagnosticId,
            "CS0535");

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id == "CS0535" &&
                    !diagnostic.GetMessage(CultureInfo.InvariantCulture)
                               .EndsWith("does not implement interface member 'IDisposable.Dispose()'"))
                {
                    continue;
                }

                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var typeDeclaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (typeDeclaration == null)
                {
                    continue;
                }

                if (diagnostic.Id == GU0031DisposeMember.DiagnosticId &&
                    Disposable.IsAssignableTo(semanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken)))
                {
                    continue;
                }

                context.RegisterCodeFix(
                CodeAction.Create(
                    "Implement IDisposable and make class sealed.",
                    cancellationToken =>
                        ApplyImplementIDisposableSealedFixAsync(
                            context,
                            semanticModel,
                            cancellationToken,
                            syntaxRoot,
                            typeDeclaration),
                    nameof(ImplementIDisposableCodeFixProvider)),
                diagnostic);
            }
        }

        private static Task<Document> ApplyImplementIDisposableSealedFixAsync(CodeFixContext context, SemanticModel semanticModel, CancellationToken cancellationToken, SyntaxNode syntaxRoot, TypeDeclarationSyntax typeDeclaration)
        {
            var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            var syntaxGenerator = SyntaxGenerator.GetGenerator(context.Document);
            SyntaxNode updated = typeDeclaration;

            IMethodSymbol existsingMethod;
            if (!type.TryGetMethod("Dispose", out existsingMethod))
            {
                IFieldSymbol existsingField;
                if (!type.TryGetField("disposed", out existsingField))
                {
                    var disposedField = syntaxGenerator.FieldDeclaration(
                        "disposed",
                        accessibility: Accessibility.Private,
                        type: SyntaxFactory.ParseTypeName("bool"));
                    MemberDeclarationSyntax field;
                    if (typeDeclaration.Members.TryGetLast(x => x is FieldDeclarationSyntax, out field))
                    {
                        updated = updated.InsertNodesAfter(field, new[] { disposedField });
                    }
                    else if (typeDeclaration.Members.TryGetFirst(out field))
                    {
                        updated = updated.InsertNodesBefore(field, new[] { disposedField });
                    }
                    else
                    {
                        updated = syntaxGenerator.AddMembers(updated, disposedField);
                    }
                }

                var ifDisposedReturn = syntaxGenerator.IfStatement(
                    SyntaxFactory.ParseExpression("this.disposed"),
                    new[] { SyntaxFactory.ReturnStatement() });
                var disposeMethod = syntaxGenerator.MethodDeclaration(
                    "Dispose",
                    accessibility: Accessibility.Public,
                    statements: new[]
                    {
                        ifDisposedReturn,
                        SyntaxFactory.ParseStatement("this.disposed = true;")
                    });

                MemberDeclarationSyntax method;
                if (typeDeclaration.Members.TryGetLast(
                                       x => (x as MethodDeclarationSyntax)?.Modifiers.Any(SyntaxKind.PublicKeyword) == true,
                                       out method))
                {
                    updated = updated.InsertNodesAfter(method, new[] { disposeMethod });
                }
                else if (typeDeclaration.Members.TryGetFirst(x => x.IsKind(SyntaxKind.MethodDeclaration), out method))
                {
                    updated = updated.InsertNodesBefore(method, new[] { disposeMethod });
                }
                else
                {
                    updated = syntaxGenerator.AddMembers(updated, disposeMethod);
                }

                if (!type.TryGetMethod("ThrowIfDisposed", out existsingMethod))
                {
                    var ifDisposedThrow = syntaxGenerator.IfStatement(
                        SyntaxFactory.ParseExpression("this.disposed"),
                        new[] { SyntaxFactory.ParseStatement("throw new ObjectDisposedException(GetType().FullName);") });
                    var throwIfDisposedMethod = syntaxGenerator.MethodDeclaration(
                        "ThrowIfDisposed",
                        accessibility: Accessibility.Protected,
                        statements: new[] { ifDisposedThrow });

                    if (typeDeclaration.Members.TryGetLast(
                                           x => (x as MethodDeclarationSyntax)?.Modifiers.Any(SyntaxKind.ProtectedKeyword) == true,
                                           out method))
                    {
                        updated = updated.InsertNodesAfter(method, new[] { throwIfDisposedMethod });
                    }
                    else
                    {
                        updated = syntaxGenerator.AddMembers(updated, throwIfDisposedMethod);
                    }
                }
            }

            if (!Disposable.IsAssignableTo(type))
            {
                updated = syntaxGenerator.AddInterfaceType(updated, IDisposableInterface);
            }

            if (!IsSealed(typeDeclaration))
            {
                updated = syntaxGenerator.WithModifiers(updated, DeclarationModifiers.Sealed);
            }

            updated = MakeSealedRewriter.Default.Visit(updated);
            var newRoot = (CompilationUnitSyntax)syntaxRoot.ReplaceNode(typeDeclaration, updated);
            UsingDirectiveSyntax @using;
            if (!newRoot.Usings.TryGetSingle(x => x.Name.ToString() == "System", out @using))
            {
                newRoot = newRoot.Usings.Any()
                    ? newRoot.InsertNodesBefore(newRoot.Usings.First(), new[] { UsingSystem })
                    : newRoot.AddUsings(UsingSystem);
            }

            return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
        }

        private static bool IsSealed(TypeDeclarationSyntax type)
        {
            return type.Modifiers.Any(SyntaxKind.SealedKeyword);
        }

        private class MakeSealedRewriter : CSharpSyntaxRewriter
        {
            public static readonly MakeSealedRewriter Default = new MakeSealedRewriter();

            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                SyntaxToken modifier;
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitFieldDeclaration(node);
            }

            public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
            {
                SyntaxToken modifier;
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitEventDeclaration(node);
            }

            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                SyntaxToken modifier;
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitPropertyDeclaration(node);
            }

            public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
            {
                SyntaxToken modifier;
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                if (node.FirstAncestorOrSelf<PropertyDeclarationSyntax>()
                        .Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.PrivateKeyword), out modifier))
                {
                    if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.PrivateKeyword), out modifier))
                    {
                        node = node.WithModifiers(node.Modifiers.Remove(modifier));
                    }
                }

                return base.VisitAccessorDeclaration(node);
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                SyntaxToken modifier;
                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.VirtualKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Remove(modifier));
                }

                if (node.Modifiers.TryGetSingle(x => x.IsKind(SyntaxKind.ProtectedKeyword), out modifier))
                {
                    node = node.WithModifiers(node.Modifiers.Replace(modifier, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                }

                return base.VisitMethodDeclaration(node);
            }
        }
    }
}