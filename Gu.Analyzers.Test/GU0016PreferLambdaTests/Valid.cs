namespace Gu.Analyzers.Test.GU0016PreferLambdaTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new MethodGroupAnalyzer();

        [Test]
        public static void LinqWhereStaticMethod()
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;
    using System.Linq;

    public class C
    {
        public C(IEnumerable<int> ints)
        {
            var meh = ints.Where(x => IsEven(x));
        }

        private static bool IsEven(int x) => x % 2 == 0;
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
