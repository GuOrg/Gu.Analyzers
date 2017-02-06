namespace Gu.Analyzers
{
    internal enum ValueSource
    {
        Unknown,
        Recursion,
        External,
        Constant,
        Created,
        PotentiallyCreated,
        Injected,
        PotentiallyInjected,
        Member,
        Cached,
        Calculated,
        Ref,
        Out
    }
}