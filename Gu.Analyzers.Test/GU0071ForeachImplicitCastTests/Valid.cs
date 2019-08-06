namespace Gu.Analyzers.Test.GU0071ForeachImplicitCastTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0071ForeachImplicitCast Analyzer = new GU0071ForeachImplicitCast();

        [TestCase("int[]")]
        [TestCase("List<int>")]
        [TestCase("IEnumerable<int>")]
        [TestCase("IEnumerable<IEnumerable<char>>")]
        public static void VarInAForeach(string type)
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;

    public class A
    {
        public void F(int[] values)
        {
            foreach(var a in values)
            {
            }
        }
    }
}".AssertReplace("int[]", type);
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExplicitTypeMatchesInAForeach()
        {
            var code = @"
namespace N
{
    using System.Collections.Generic;

    public class A
    {
        public void F()
        {
            IEnumerable<IEnumerable<char>> b = new string[0];
            foreach (IEnumerable<char> a in b)
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MultipleIEnumerableInterfaces()
        {
            var testCode = @"
namespace N
{
    using System.Collections;
    using System.Collections.Generic;

    class C : IEnumerable<IEnumerable<char>>, IEnumerable<int>
    {
        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            yield return 42;
        }

        void GetEnumerator(int x)
        {
        }

        IEnumerator<IEnumerable<char>> IEnumerable<IEnumerable<char>>.GetEnumerator()
        {
            yield return string.Empty;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IEnumerable<char>>)this).GetEnumerator();
        }
    }

    public class A
    {
        public void F()
        {
            foreach(int a in new C())
            {
            }
        }
    }
}";
            var sln = CodeFactory.CreateSolution(testCode, CodeFactory.DefaultCompilationOptions(Analyzer), MetadataReferences.FromAttributes());
            var diagnostics = Analyze.GetDiagnostics(Analyzer, sln);
            RoslynAssert.NoDiagnostics(diagnostics);
        }

        [Test]
        public static void ExplicitTypeWhenLoopingRegexMatches()
        {
            var code = @"
namespace N
{
    using System.Text.RegularExpressions;

    class C
    {
        public C()
        {
            foreach (Match match in Regex.Matches(string.Empty, string.Empty))
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DuckTypedEnumerable()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    class C
    {
        public IEnumerator<int> GetEnumerator()
        {
            yield return 1;
        }
    }

    public class A
    {
        public void F()
        {
            foreach(int a in new C())
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void NonGenericIEnumerable()
        {
            var code = @"
namespace N
{
    using System.Collections;

    class C<T>
    {
        private void M(IEnumerable enumerable)
        {
            foreach (T item in enumerable)
            {
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
