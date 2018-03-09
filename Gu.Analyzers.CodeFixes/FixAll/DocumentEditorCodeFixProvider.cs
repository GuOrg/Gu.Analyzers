namespace Gu.Analyzers
{
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CodeFixes;

    public abstract class DocumentEditorCodeFixProvider : CodeFixProvider
    {
        protected virtual DocumentEditorFixAllProvider FixAllProvider() => DocumentEditorFixAllProvider.Default;

        public sealed override FixAllProvider GetFixAllProvider() => this.FixAllProvider();

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            return this.RegisterCodeFixesAsync(new DocumentEditorCodeFixContext(context));
        }

        public abstract Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context);
    }
}
