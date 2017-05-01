namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal static class DocumentEditorExt
    {
        internal static void AddField(this DocumentEditor editor, TypeDeclarationSyntax containingType, FieldDeclarationSyntax field)
        {
            FieldDeclarationSyntax existing = null;
            foreach (var member in containingType.Members)
            {
                if (member is FieldDeclarationSyntax fieldDeclaration)
                {
                    if (IsInsertBefore(fieldDeclaration))
                    {
                        editor.InsertBefore(fieldDeclaration, field);
                        return;
                    }

                    existing = fieldDeclaration;
                    continue;
                }

                editor.InsertBefore(member, field);
                return;
            }

            if (existing != null)
            {
                editor.InsertAfter(existing, field);
            }
            else
            {
                editor.AddMember(containingType, field);
            }
        }

        private static bool IsInsertBefore(FieldDeclarationSyntax existing)
        {
            if (!existing.IsPrivate() ||
                existing.IsStatic())
            {
                return false;
            }

            return true;
        }

        private static bool IsPrivate(this FieldDeclarationSyntax field)
        {
            foreach (var modifier in field.Modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.PrivateKeyword:
                        return true;
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.PublicKeyword:
                        return false;
                }
            }

            return true;
        }

        private static bool IsStatic(this FieldDeclarationSyntax field)
        {
            foreach (var modifier in field.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.StaticKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsReadOnly(this FieldDeclarationSyntax field)
        {
            foreach (var modifier in field.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.ReadOnlyKeyword))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
