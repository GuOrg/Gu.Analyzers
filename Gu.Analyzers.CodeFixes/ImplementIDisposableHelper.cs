namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static class ImplementIDisposableHelper
    {
        private static readonly TypeSyntax IDisposableInterface = SyntaxFactory.ParseTypeName("IDisposable");
        private static readonly UsingDirectiveSyntax UsingSystem = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System"));

        internal static TypeDeclarationSyntax WithDisposedField(this TypeDeclarationSyntax typeDeclaration, ITypeSymbol type, SyntaxGenerator syntaxGenerator, bool usesUnderscoreNames)
        {
            var disposedFieldName = usesUnderscoreNames
                                        ? "_disposed"
                                        : "disposed";

            IFieldSymbol existsingField;
            if (!type.TryGetField(disposedFieldName, out existsingField))
            {
                var disposedField = syntaxGenerator.FieldDeclaration(
                    disposedFieldName,
                    accessibility: Accessibility.Private,
                    type: SyntaxFactory.ParseTypeName("bool"));

                var members = typeDeclaration.Members;
                MemberDeclarationSyntax field;
                if (members.TryGetLast(x => x is FieldDeclarationSyntax, out field))
                {
                    return typeDeclaration.InsertNodesAfter(field, new[] { disposedField });
                }

                if (members.TryGetFirst(out field))
                {
                    return typeDeclaration.InsertNodesBefore(field, new[] { disposedField });
                }

                return (TypeDeclarationSyntax)syntaxGenerator.AddMembers(typeDeclaration, disposedField);
            }

            return typeDeclaration;
        }

        internal static TypeDeclarationSyntax WithThrowIfDisposed(this TypeDeclarationSyntax typeDeclaration, ITypeSymbol type, SyntaxGenerator syntaxGenerator, bool usesUnderscoreNames)
        {
            IMethodSymbol existsingMethod;
            if (!type.TryGetMethod("ThrowIfDisposed", out existsingMethod))
            {
                var ifDisposedThrow = syntaxGenerator.IfStatement(
                    SyntaxFactory.ParseExpression(usesUnderscoreNames ? "_disposed" : "this.disposed"),
                    new[] { SyntaxFactory.ParseStatement("throw new ObjectDisposedException(GetType().FullName);") });
                var throwIfDisposedMethod = syntaxGenerator.MethodDeclaration(
                    "ThrowIfDisposed",
                    accessibility: type.IsSealed ? Accessibility.Private : Accessibility.Protected,
                    statements: new[] { ifDisposedThrow });

                MemberDeclarationSyntax method;
                if (typeDeclaration.Members.TryGetLast(
                                       x => (x as MethodDeclarationSyntax)?.Modifiers.Any(SyntaxKind.ProtectedKeyword) == true,
                                       out method))
                {
                    return typeDeclaration.InsertNodesAfter(method, new[] { throwIfDisposedMethod });
                }

                return (TypeDeclarationSyntax)syntaxGenerator.AddMembers(typeDeclaration, throwIfDisposedMethod);
            }

            return typeDeclaration;
        }

        internal static TypeDeclarationSyntax WithIDisposableInterface(this TypeDeclarationSyntax typeDeclaration, SyntaxGenerator syntaxGenerator, ITypeSymbol type)
        {
            if (!Disposable.IsAssignableTo(type))
            {
                return (TypeDeclarationSyntax)syntaxGenerator.AddInterfaceType(typeDeclaration, IDisposableInterface);
            }

            return typeDeclaration;
        }

        internal static CompilationUnitSyntax WithUsingSystem(this CompilationUnitSyntax syntaxRoot)
        {
            var @namespace = syntaxRoot.Members.FirstOrDefault() as NamespaceDeclarationSyntax;
            if (@namespace == null)
            {
                if (syntaxRoot.Usings.HasUsingSystem())
                {
                    return syntaxRoot;
                }

                return syntaxRoot.Usings.Any()
                           ? syntaxRoot.InsertNodesBefore(syntaxRoot.Usings.First(), new[] { UsingSystem })
                           : syntaxRoot.AddUsings(UsingSystem);
            }

            if (@namespace.Usings.HasUsingSystem() || syntaxRoot.Usings.HasUsingSystem())
            {
                return syntaxRoot;
            }

            if (@namespace.Usings.Any())
            {
                return syntaxRoot.ReplaceNode(@namespace, @namespace.InsertNodesBefore(@namespace.Usings.First(), new[] { UsingSystem }));
            }

            if (syntaxRoot.Usings.Any())
            {
                return syntaxRoot.InsertNodesBefore(syntaxRoot.Usings.First(), new[] { UsingSystem });
            }

            return syntaxRoot.ReplaceNode(@namespace, @namespace.AddUsings(UsingSystem));
        }

        internal static IfStatementSyntax IfDisposedReturn(this SyntaxGenerator syntaxGenerator, bool usesUnderscoreNames)
        {
            if (usesUnderscoreNames)
            {
                return
                    (IfStatementSyntax)
                    syntaxGenerator.IfStatement(
                        SyntaxFactory.ParseExpression("_disposed"),
                        new[] { SyntaxFactory.ReturnStatement() });
            }

            return (IfStatementSyntax)syntaxGenerator.IfStatement(
                SyntaxFactory.ParseExpression("this.disposed"),
                new[] { SyntaxFactory.ReturnStatement() });
        }

        internal static IfStatementSyntax IfDisposing(this SyntaxGenerator syntaxGenerator)
        {
            return
                (IfStatementSyntax)
                syntaxGenerator.IfStatement(
                    SyntaxFactory.ParseExpression("disposing"),
                    new EmptyStatementSyntax[0]);
        }

        // ReSharper disable once UnusedParameter.Global
        internal static StatementSyntax SetDisposedTrue(this SyntaxGenerator syntaxGenerator, bool usesUnderscoreNames)
        {
            if (usesUnderscoreNames)
            {
                return SyntaxFactory.ParseStatement("_disposed = true;");
            }

            return SyntaxFactory.ParseStatement("this.disposed = true;");
        }

        internal static bool UsesUnderscoreNames(this TypeDeclarationSyntax type)
        {
            foreach (var member in type.Members)
            {
                var field = member as FieldDeclarationSyntax;
                if (field == null)
                {
                    continue;
                }

                foreach (var variable in field.Declaration.Variables)
                {
                    if (variable.Identifier.ValueText.StartsWith("_"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasUsingSystem(this SyntaxList<UsingDirectiveSyntax> usings)
        {
            foreach (var @using in usings)
            {
                if (@using.Name.ToString() == "System")
                {
                    return true;
                }
            }

            return false;
        }
    }
}