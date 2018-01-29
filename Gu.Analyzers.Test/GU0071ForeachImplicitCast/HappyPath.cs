namespace Gu.Analyzers.Test.GU0071ForeachImplicitCast
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly Analyzers.GU0071ForeachImplicitCast Analyzer = new Analyzers.GU0071ForeachImplicitCast();

        [Test]
        public void VarInAForeach()
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
            foreach(var a in b)
            {
                DoSomething(x);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ExplicitTypeMatchesInAForeach()
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
            foreach(IEnumerable<char> a in b)
            {
                DoSomething(x);
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}