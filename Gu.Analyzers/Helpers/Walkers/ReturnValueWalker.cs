namespace Gu.Analyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ReturnValueWalker : PooledWalker<ReturnValueWalker>, IReadOnlyList<ExpressionSyntax>
    {
        private readonly List<ExpressionSyntax> returnValues = new List<ExpressionSyntax>();

        private ReturnValueWalker()
        {
        }

        public int Count => this.returnValues.Count;

        public ExpressionSyntax this[int index] => this.returnValues[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.returnValues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.LocalFunctionStatement:
                    return;
                default:
                    base.Visit(node);
                    break;
            }
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            this.returnValues.Add(node.Expression);
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            this.returnValues.Add(node.Expression);
        }

        internal static bool TrySingle(SyntaxNode scope, [NotNullWhen(true)] out ExpressionSyntax? returnValue)
        {
            if (scope is null)
            {
                returnValue = null;
                return false;
            }

            using (var walker = BorrowAndVisit(scope, () => new ReturnValueWalker()))
            {
                return walker.returnValues.TrySingle(out returnValue);
            }
        }

        protected override void Clear()
        {
            this.returnValues.Clear();
        }
    }
}
