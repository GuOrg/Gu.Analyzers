namespace Gu.Analyzers
{
    using Microsoft.CodeAnalysis;

    internal static class AccessibilityExt
    {
        internal static bool IsEither(this Accessibility accessibility, Accessibility x, Accessibility y) =>
            accessibility == x || accessibility == y;
    }
}