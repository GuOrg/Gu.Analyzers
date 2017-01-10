#pragma warning disable 660,661 // using a hack with operator overloads
namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal class QualifiedMember<T>
        where T : ISymbol
    {
        internal readonly string Name;
        internal readonly QualifiedType ContainingType;

        public QualifiedMember(QualifiedType containingType, string name)
        {
            this.Name = name;
            this.ContainingType = containingType;
        }

        public static bool operator ==(T left, QualifiedMember<T> right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            return (left.Name == right.Name &&
                   left.ContainingType == right.ContainingType) ||
                   left.Name.IsParts(right.ContainingType.FullName, ".", right.Name);
        }

        public static bool operator !=(T left, QualifiedMember<T> right) => !(left == right);

        public static bool operator ==(ISymbol left, QualifiedMember<T> right)
        {
            return left is T && (T)left == right;
        }

        public static bool operator !=(ISymbol left, QualifiedMember<T> right)
        {
            return !(left == right);
        }
    }
}