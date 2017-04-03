namespace Gu.Analyzers
{
    internal static class ResultExt
    {
        internal static bool IsEither(this Result result, Result first, Result other) => result == first || result == other;
    }
}