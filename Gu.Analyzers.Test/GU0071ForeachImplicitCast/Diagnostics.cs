namespace Gu.Analyzers.Test.GU0071ForeachImplicitCast
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        [Test]
        public void GenericCollectionWithACast()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    public class A
    {
        public void F()
        {
            IEnumerable<IEnumerable<char>> b = new[]{""lol"", ""asdf"", ""test""};
            foreach(↓List<char> a in b)
            {
                DoSomething(x);
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics<Analyzers.GU0071ForeachImplicitCast>(testCode);
        }
    }
}