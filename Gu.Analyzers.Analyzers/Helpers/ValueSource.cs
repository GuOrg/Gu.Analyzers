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
        Argument,
        Injected,
        PotentiallyInjected,
        Member,
        Cached,
        Calculated,
        Ref,
        Out
    }
}