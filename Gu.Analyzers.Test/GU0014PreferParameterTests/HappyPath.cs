namespace Gu.Analyzers.Test.GU0014PreferParameterTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly ConstructorAnalyzer Analyzer = new ConstructorAnalyzer();

        [Test]
        public void WhenDefaultValue()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}