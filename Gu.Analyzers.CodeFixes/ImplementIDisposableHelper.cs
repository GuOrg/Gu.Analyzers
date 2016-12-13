namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static class ImplementIDisposableHelper
    {
        internal static SyntaxNode WithDisposedField(this TypeDeclarationSyntax typeDeclaration, ITypeSymbol type, SyntaxGenerator syntaxGenerator, bool usesUnderscoreNames)
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

                return syntaxGenerator.AddMembers(typeDeclaration, disposedField);
            }

            return typeDeclaration;
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

        internal static SyntaxNode WithThrowIfDisposed(this TypeDeclarationSyntax typeDeclaration, ITypeSymbol type, SyntaxGenerator syntaxGenerator, bool usesUnderscoreNames)
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

                return syntaxGenerator.AddMembers(typeDeclaration, throwIfDisposedMethod);
            }

            return typeDeclaration;
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
    }
}