namespace Gu.Analyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class MemberPathWalker : CSharpSyntaxWalker, IReadOnlyList<IdentifierNameSyntax>
    {
        private static readonly Pool<MemberPathWalker> Pool = new Pool<MemberPathWalker>(
            () => new MemberPathWalker(),
            x =>
                {
                    x.names.Clear();
                });

        private readonly List<IdentifierNameSyntax> names = new List<IdentifierNameSyntax>();

        private MemberPathWalker()
        {
        }

        public int Count => this.names.Count;

        public IdentifierNameSyntax this[int index] => this.names[index];

        public IEnumerator<IdentifierNameSyntax> GetEnumerator()
        {
            return this.names.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.names).GetEnumerator();
        }

        public override void VisitCastExpression(CastExpressionSyntax node)
        {
            this.Visit(node.Expression);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.AsExpression:
                    this.Visit(node.Left);
                    return;
            }

            base.VisitBinaryExpression(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.names.Add(node);
            base.VisitIdentifierName(node);
        }

        internal static Pool<MemberPathWalker>.Pooled Create(ExpressionStatementSyntax node)
        {
            var pooled = Pool.GetOrCreate();
            if (node != null)
            {
                pooled.Item.Visit(node);
            }

            return pooled;
        }
    }
}