namespace Gu.Analyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, fooCode }, EmptyDiagnosticResults).ConfigureAwait(false);
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