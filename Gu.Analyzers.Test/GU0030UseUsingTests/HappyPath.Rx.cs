namespace Gu.Analyzers.Test.GU0030UseUsingTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath : HappyPathVerifier<GU0030UseUsing>
    {
        internal class Rx : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task CompositeDisposableInitializer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly IDisposable disposable;

        public Foo()
        {
            this.disposable = new CompositeDisposable { File.OpenRead(string.Empty) };
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task CompositeDisposableAdd()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly CompositeDisposable disposable = new CompositeDisposable();

        public Foo()
        {
            this.disposable.Add(File.OpenRead(string.Empty));
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";

                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

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
            public async Task SerialDisposableObjectInitializer()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public sealed class Foo : IDisposable
    {
        private readonly SerialDisposable disposable;

        public Foo()
        {
            this.disposable = new SerialDisposable { Disposable = File.OpenRead(string.Empty) };
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
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