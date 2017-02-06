namespace Gu.Analyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    public class HappyPathWithAll : DiagnosticVerifier
    {
        [Test]
        public void NotEmpty()
        {
            CollectionAssert.IsNotEmpty(this.GetCSharpDiagnosticAnalyzers());
            Assert.Pass($"Count: {this.GetCSharpDiagnosticAnalyzers().Count()}");
        }

        public override void IdMatches()
        {
            Assert.Pass();
        }

        ////[Explicit("Temporarily ignore")]
        [Test]
        public async Task SomewhatRealisticSample()
        {
            var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public Disposable(string meh)
        : this()
    {
    }

    public Disposable()
    {
    }

    public void Dispose()
    {
    }
}";

            var fooCode = @"
using System;
using System.ComponentModel;
using System.IO;
using System.Reactive.Disposables;

public class Foo : IDisposable
{
    private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
    private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

    private IDisposable meh1;
    private IDisposable meh2;
    private bool isDirty;

    public Foo()
    {
        this.meh1 = this.RecursiveProperty;
        this.meh2 = this.RecursiveMethod();
        this.subscription.Disposable = File.OpenRead(string.Empty);
    }

    public event PropertyChangedEventHandler PropertyChanged
    {
        add { this.PropertyChangedCore += value; }
        remove { this.PropertyChangedCore -= value; }
    }

    private event PropertyChangedEventHandler PropertyChangedCore;

    public Disposable RecursiveProperty => RecursiveProperty;

    public IDisposable Disposable => subscription.Disposable;

    public bool IsDirty
    {
        get
        {
            return this.isDirty;
        }

        private set
        {
            if (value == this.isDirty)
            {
                return;
            }

            this.isDirty = value;
            this.PropertyChangedCore?.Invoke(this, IsDirtyPropertyChangedEventArgs);
        }
    }

    public Disposable RecursiveMethod() => RecursiveMethod();

    public void Meh()
    {
        using (var item = new Disposable())
        {
        }

        using (var item = RecursiveProperty)
        {
        }

        using (RecursiveProperty)
        {
        }

        using (var item = RecursiveMethod())
        {
        }

        using (RecursiveMethod())
        {
        }
    }

    public void Dispose()
    {
        this.subscription.Dispose();
    }
}";

            var fooBaseCode = @"
    using System;
    using System.IO;

    public abstract class FooBase : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed = false;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                this.stream.Dispose();
            }
        }
    }";

            var fooImplCode = @"
    using System;
    using System.IO;

    public class FooImpl : FooBase
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }";

            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, fooCode, fooBaseCode, fooImplCode }, EmptyDiagnosticResults).ConfigureAwait(false);
        }

        [Test]
        public async Task WithSyntaxcErrors()
        {
            var syntaxErrorCode = @"
    using System;
    using System.IO;

    public class Foo : SyntaxError
    {
        private readonly Stream stream = File.SyntaxError(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.syntaxError)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }";
            var analyzers = this.GetCSharpDiagnosticAnalyzers().ToImmutableArray();
            await GetSortedDiagnosticsFromDocumentsAsync(
                          analyzers,
                          CodeFactory.GetDocuments(
                              new[] { syntaxErrorCode },
                              analyzers,
                              Enumerable.Empty<string>()),
                          CancellationToken.None)
                      .ConfigureAwait(false);
        }

        internal override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            return typeof(KnownSymbol).Assembly
                                      .GetTypes()
                                      .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                                      .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t));
        }
    }
}