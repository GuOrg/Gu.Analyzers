namespace Gu.Analyzers.Test.GU0016PreferLambdaTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ArgumentAnalyzer();

        [Test]
        public void LinqWhereStaticMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.Linq;

    public class Foo
    {
        public Foo(IEnumerable<int> ints)
        {
            var meh = ints.Where(x => IsEven(x));
        }

        private static bool IsEven(int x) => x % 2 == 0;
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
