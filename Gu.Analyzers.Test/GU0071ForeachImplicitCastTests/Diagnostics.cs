namespace Gu.Analyzers.Test.GU0071ForeachImplicitCastTests
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
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics<Analyzers.GU0071ForeachImplicitCast>(testCode);
        }

        [Test]
        public void GenericCollectionWithAnExplicitImplementation()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    class Lol : IEnumerable<IEnumerable<char>>
    {
        /// <inheritdoc />
        IEnumerator<IEnumerable<char>> IEnumerable<IEnumerable<char>>.GetEnumerator()
        {
            yield return ""lol"";
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
            AnalyzerAssert.Diagnostics<Analyzers.GU0071ForeachImplicitCast>(testCode);
        }

        [Test]
        public void Array()
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
            foreach(↓List<char> a in new IEnumerable<char>[] { ""lol"", ""asdf"", ""test"" })
            {
            }
        }
    }
}";
            AnalyzerAssert.Diagnostics<Analyzers.GU0071ForeachImplicitCast>(testCode);
        }

        [Test]
        public void DuckTypedEnumerable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;

    class Lol
    {
        IEnumerator<IEnumerable<char>> GetEnumerator()
        {
            yield return ""lol"";
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
            AnalyzerAssert.Diagnostics<Analyzers.GU0071ForeachImplicitCast>(testCode);
        }
    }
}