namespace Gu.Analyzers.Test.GU0035ImplementIDisposableTests
{
    internal partial class HappyPath
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