namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class AccessibilityExt
    {
        internal static bool IsEither(this Accessibility accessibility, Accessibility x, Accessibility y) => accessibility == x || accessibility == y;

        internal static bool IsEither(this Accessibility accessibility, Accessibility x, Accessibility y, Accessibility z) => accessibility == x || accessibility == y || accessibility == z;
    }
}