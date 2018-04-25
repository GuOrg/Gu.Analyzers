namespace Gu.Analyzers
{
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Attribute
    {
        internal static bool TryGetAttribute(SyntaxList<AttributeListSyntax> attributeLists, QualifiedType attributeType, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax result)
        {
            result = null;
            foreach (var attributeList in attributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (IsType(attribute, attributeType, semanticModel, cancellationToken))
                    {
                        result = attribute;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsType(AttributeSyntax attribute, QualifiedType qualifiedType, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (attribute == null)
            {
                return false;
            }

            if (attribute.Name is SimpleNameSyntax simpleName)
            {
                if (!IsMatch(simpleName, qualifiedType) &&
                    !AliasWalker.Contains(attribute.SyntaxTree, simpleName.Identifier.ValueText))
                {
                    return false;
                }
            }
            else if (attribute.Name is QualifiedNameSyntax qualifiedName &&
                     qualifiedName.Right is SimpleNameSyntax typeName)
            {
                if (!IsMatch(typeName, qualifiedType) &&
                    !AliasWalker.Contains(attribute.SyntaxTree, typeName.Identifier.ValueText))
                {
                    return false;
                }
            }

            var attributeType = semanticModel.GetTypeInfoSafe(attribute, cancellationToken).Type;
            return attributeType == qualifiedType;

            bool IsMatch(SimpleNameSyntax sn, QualifiedType qt)
            {
                return sn.Identifier.ValueText == qt.Type ||
                       qt.Type.IsParts(sn.Identifier.ValueText, "Attribute");
            }
        }

        internal static bool TryGetTypeName(AttributeSyntax attribute, out string name)
        {
            name = null;
            if (attribute == null)
            {
                return false;
            }

            if (attribute.Name is SimpleNameSyntax simpleName)
            {
                name = simpleName.Identifier.ValueText;
                return true;
            }

            if (attribute.Name is QualifiedNameSyntax qualifiedName &&
                qualifiedName.Right is SimpleNameSyntax typeName)
            {
                name = typeName.Identifier.ValueText;
                return true;
            }

            return false;
        }
    }
}
