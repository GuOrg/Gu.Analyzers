namespace Gu.Analyzers;

using System.Collections.Generic;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal sealed class ObjectCreationWalker : PooledWalker<ObjectCreationWalker>
{
    private readonly List<ObjectCreationExpressionSyntax> objectCreations = new();

    private ObjectCreationWalker()
    {
    }

    internal IReadOnlyList<ObjectCreationExpressionSyntax> ObjectCreations => this.objectCreations;

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        this.objectCreations.Add(node);
        base.VisitObjectCreationExpression(node);
    }

    internal static ObjectCreationWalker BorrowAndVisit(SyntaxNode scope) => BorrowAndVisit(scope, () => new ObjectCreationWalker());

    protected override void Clear()
    {
        this.objectCreations.Clear();
    }
}