namespace Gu.Analyzers
{
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
                    if (IsInsertBefore(existing, field))
                    {
                        editor.InsertBefore(existing, field);
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

        private static bool IsInsertBefore(FieldDeclarationSyntax existing, FieldDeclarationSyntax field)
        {
            return true;
        }
    }
}
