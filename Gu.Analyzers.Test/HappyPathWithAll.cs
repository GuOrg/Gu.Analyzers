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

public class Foo
{
    private IDisposable meh1;
    private IDisposable meh2;

    public Foo()
    {
        this.meh1 = this.RecursiveProperty;
        this.meh2 = this.RecursiveMethod();
    }

    public Disposable RecursiveProperty => RecursiveProperty;

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
}";
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, fooCode }, EmptyDiagnosticResults).ConfigureAwait(false);
        }

        internal override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            return typeof(Gu.Analyzers.KnownSymbol).Assembly.GetTypes()
                                                   .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
                                                   .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t));
        }
    }
}