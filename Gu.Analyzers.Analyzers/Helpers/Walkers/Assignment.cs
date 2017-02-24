namespace Gu.Analyzers
{
    using System.Diagnostics;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [DebuggerDisplay("Symbol: {this.Symbol} Value: {this.Value}")]
    internal struct Assignment
    {
        internal readonly ISymbol Symbol;
        internal readonly ExpressionSyntax Value;

        public Assignment(ISymbol symbol, ExpressionSyntax value)
        {
            this.Symbol = symbol;
            this.Value = value;
        }
    }
}