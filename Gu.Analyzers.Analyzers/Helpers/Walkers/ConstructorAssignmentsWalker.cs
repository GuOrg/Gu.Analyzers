namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ConstructorAssignmentsWalker : CSharpSyntaxWalker
    {
        internal readonly Dictionary<ParameterSyntax, string> ParameterNameMap = new Dictionary<ParameterSyntax, string>();

        private static readonly Pool<ConstructorAssignmentsWalker> Cache = new Pool<ConstructorAssignmentsWalker>(
            () => new ConstructorAssignmentsWalker(),
            x =>
            {
                x.ParameterNameMap.Clear();
                x.constructor = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private ConstructorDeclarationSyntax constructor;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private ConstructorAssignmentsWalker()
        {
        }

        public static Pool<ConstructorAssignmentsWalker>.Pooled Create(
            ConstructorDeclarationSyntax constructor,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.constructor = constructor;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.Visit(constructor);
            return pooled;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var right = node.Right as IdentifierNameSyntax;
            if (right != null)
            {
                ParameterSyntax match;
                if (this.constructor.ParameterList.Parameters.TryGetSingle(x => x.Identifier.ValueText == right.Identifier.ValueText, out match))
                {
                    var symbol = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken);
                    if (this.ParameterNameMap.ContainsKey(match))
                    {
                        this.ParameterNameMap[match] = null;
                    }
                    else
                    {
                        this.ParameterNameMap.Add(match, ParameterName(symbol));
                    }
                }
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            var ctor = this.semanticModel.GetSymbolSafe(node, this.cancellationToken) as IMethodSymbol;
            if (ctor != null)
            {
                for (var i = 0; i < node.ArgumentList.Arguments.Count; i++)
                {
                    var arg = node.ArgumentList.Arguments[i].Expression as IdentifierNameSyntax;
                    ParameterSyntax match;
                    if (this.constructor.ParameterList.Parameters.TryGetSingle(x => x.Identifier.ValueText == arg?.Identifier.ValueText, out match))
                    {
                        if (this.ParameterNameMap.ContainsKey(match))
                        {
                            this.ParameterNameMap[match] = null;
                        }
                        else
                        {
                            if (ctor.Parameters.Length - 1 <= i &&
                                ctor.Parameters[ctor.Parameters.Length - 1].IsParams)
                            {
                                this.ParameterNameMap.Add(match, null);
                            }
                            else
                            {
                                this.ParameterNameMap.Add(match, ctor.Parameters[i].Name);
                            }
                        }
                    }
                }
            }

            base.VisitConstructorInitializer(node);
        }

        private static string ParameterName(ISymbol symbol)
        {
            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null)
            {
                if (IsAllCaps(fieldSymbol.Name))
                {
                    return fieldSymbol.Name.ToLowerInvariant();
                }

                return FirstCharLowercase(TrimLeadingUnderscore(fieldSymbol.Name));
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null)
            {
                if (IsAllCaps(propertySymbol.Name))
                {
                    return propertySymbol.Name.ToLowerInvariant();
                }

                return FirstCharLowercase(propertySymbol.Name);
            }

            return null;
        }

        private static bool IsAllCaps(string name)
        {
            foreach (var c in name)
            {
                if (char.IsLetter(c) && char.IsLower(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static string TrimLeadingUnderscore(string text)
        {
            if (text[0] != '_' || text.Length == 1)
            {
                return text;
            }

            return text.Substring(1);
        }

        private static string FirstCharLowercase(string text)
        {
            if (char.IsLower(text[0]))
            {
                return text;
            }

            var charArray = text.ToCharArray();
            charArray[0] = char.ToLower(charArray[0]);
            return new string(charArray);
        }
    }
}