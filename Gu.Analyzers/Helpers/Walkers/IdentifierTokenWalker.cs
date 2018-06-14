namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    /// <summary>
    /// Get all <see cref="SyntaxToken"/> in the scope
    /// </summary>
    internal sealed class IdentifierTokenWalker : PooledWalker<IdentifierTokenWalker>
    {
        private readonly List<SyntaxToken> identifierTokens = new List<SyntaxToken>();

        private IdentifierTokenWalker()
            : base(SyntaxWalkerDepth.Token)
        {
        }

        /// <summary>
        /// Gets the <see cref="SyntaxToken"/>s found in the scope.
        /// </summary>
        public IReadOnlyList<SyntaxToken> IdentifierTokens => this.identifierTokens;

        /// <summary>
        /// Get a walker that has visited <paramref name="node"/>
        /// </summary>
        /// <param name="node">The scope</param>
        /// <returns>A walker that has visited <paramref name="node"/></returns>
        public static IdentifierTokenWalker Borrow(SyntaxNode node) => BorrowAndVisit(node, () => new IdentifierTokenWalker());

        public override void VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.IdentifierToken))
            {
                this.identifierTokens.Add(token);
            }

            base.VisitToken(token);
        }

        /// <summary>
        /// Try find an <see cref="SyntaxToken"/> by name
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="identifierToken">The <see cref="SyntaxToken"/></param>
        /// <returns>True if a match was found</returns>
        public bool TryFind(string name, out SyntaxToken identifierToken)
        {
            foreach (var candidate in this.IdentifierTokens)
            {
                if (candidate.ValueText == name)
                {
                    identifierToken = candidate;
                    return true;
                }
            }

            identifierToken = default(SyntaxToken);
            return false;
        }

        /// <inheritdoc />
        protected override void Clear()
        {
            this.identifierTokens.Clear();
        }
    }
}
