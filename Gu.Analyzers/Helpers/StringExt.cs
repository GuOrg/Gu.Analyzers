namespace Gu.Analyzers
{
    internal static class StringExt
    {
        internal static string Slice(this string text, int start, int end) => text[start..end];
    }
}
