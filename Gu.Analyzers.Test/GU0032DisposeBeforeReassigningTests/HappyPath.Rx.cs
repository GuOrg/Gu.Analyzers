namespace Gu.Analyzers.Test.GU0032DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0032DisposeBeforeReassigning>
    {
        internal class Rx : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task SerialDisposable()
            {
                var testCode = @"
using System;
using System.IO;
using System.Reactive.Disposables;

public sealed class Foo : IDisposable
{
    private readonly SerialDisposable disposable = new SerialDisposable();

    public void Update()
    {
        this.disposable.Disposable = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";

                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task SingleAssignmentDisposable()
            {
                var testCode = @"
using System;
using System.IO;
using System.Reactive.Disposables;

public sealed class Foo : IDisposable
{
    private readonly SingleAssignmentDisposable disposable = new SingleAssignmentDisposable();

    public void Update()
    {
        this.disposable.Disposable = File.OpenRead(string.Empty);
    }

    public void Dispose()
    {
        this.disposable.Dispose();
    }
}";

                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }
        }
    }
}