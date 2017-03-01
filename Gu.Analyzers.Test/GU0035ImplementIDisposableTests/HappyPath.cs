namespace Gu.Analyzers.Test.GU0035ImplementIDisposableTests
{
    internal partial class HappyPath : HappyPathVerifier<GU0035ImplementIDisposable>
    {
        private static readonly string DisposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";
    }
}