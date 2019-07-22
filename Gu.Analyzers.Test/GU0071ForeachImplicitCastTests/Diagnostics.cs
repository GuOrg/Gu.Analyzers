namespace Gu.Analyzers.Test.GU0071ForeachImplicitCastTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0071ForeachImplicitCast Analyzer = new GU0071ForeachImplicitCast();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0071ForeachImplicitCast);

        [TestCase("int[]")]
        [TestCase("System.Collections.Generic.List<int>")]
        [TestCase("System.Collections.Generic.IEnumerable<int>")]
        public static void ExplicitDouble(string type)
        {
            var code = @"
namespace N
{
    public class A
    {
        public void F(int[] values)
        {
            foreach(double d in values)
            {
            }
        }
    }
}".AssertReplace("int[]", type);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void GenericCollectionWithACast()
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
            foreach(↓List<char> a in b)
            {
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void GenericCollectionWithAnExplicitImplementation()
        {
            var code = @"
namespace N
{
    using System.Collections;
    using System.Collections.Generic;

    class Lol : IEnumerable<IEnumerable<char>>
    {
        /// <inheritdoc />
        IEnumerator<IEnumerable<char>> IEnumerable<IEnumerable<char>>.GetEnumerator()
        {
            yield return string.Empty;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IEnumerable<char>>)this).GetEnumerator();
        }
    }

    public class A
    {
        public void F()
        {
            foreach(↓List<char> a in new Lol())
            {
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void Array()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class A
    {
        public void F()
        {
            foreach(↓List<char> a in new IEnumerable<char>[] { ""lol"", ""asdf"", ""test"" })
            {
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Test]
        public static void DuckTypedEnumerable()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    class Lol
    {
        public IEnumerator<IEnumerable<char>> GetEnumerator()
        {
            yield return string.Empty;
        }
    }

    public class A
    {
        public void F()
        {
            foreach(↓List<char> a in new Lol())
            {
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
