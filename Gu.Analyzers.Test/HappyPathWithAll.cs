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
public class Foo
{
    public Disposable RecursiveProperty => RecursiveProperty;
    public Disposable RecursiveMethod() => RecursiveMethod();

    public void Meh()
    {
        using(var item = new Disposable())
        {
        }

        using(var item = RecursiveProperty)
        {
        }

        using(RecursiveProperty)
        {
        }

        using(var item = RecursiveMethod())
        {
        }

        using(RecursiveMethod())
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