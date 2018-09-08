namespace Gu.Analyzers.Test.GU0003CtorParameterNamesShouldMatchTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ConstructorAnalyzer();

        [Test]
        public void ConstructorSettingProperties()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingPropertiesStruct()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public struct Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ChainedConstructorSettingProperties()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c)
            : this(a, b, c, 1)
        {
        }

        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void BaseConstructorCall()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, fooCode, barCode);
        }

        [Test]
        public void ConstructorSettingField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int b;
        private readonly int c;
        private readonly int d;

        public Foo(int a, int b, int c, int d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ConstructorSettingFieldPrefixedByUnderscore()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int _a;
        private readonly int _b;
        private readonly int _c;
        private readonly int _d;

        public Foo(int a, int b, int c, int d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresWhenSettingTwoProperties()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
            this.B = a;
        }

        public int A { get; }

        public int B { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresWhenBaseIsParams()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(params int[] values)
        {
            this.Values = values;
        }

        public int[] Values { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, fooCode, barCode);
        }

        [Test]
        public void IgnoresWhenBaseIsParams2()
        {
            var fooCode = @"
namespace RoslynSandbox
{
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }
}";
            var barCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, params int[] values)
        {
            this.A = a;
            this.Values = values;
        }

        public int A { get; }

        public int[] Values { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, fooCode, barCode);
        }

        [Test]
        public void IgnoresIdCaps()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int id)
        {
            this.ID = id;
        }

        public int ID { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresTupleCreate()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            var tuple = Tuple.Create(
                1,
                2,
                3,
                4);
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoresNumbered()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo : Bar
    {
        public Foo(int x, int y, int z)
            : base(x, y, z)
        {
        }
    }

    public class Bar
    {
        public Bar(int value1, int value2, int value3)
        {
            this.Values = new int[] { value1, value2, value3 };
        }

        public int[] Values { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoredWhenAssigningWeakReferenceTarget()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Text;

    public class Foo
    {
        private readonly WeakReference wr = new WeakReference(null);

        public Foo(StringBuilder builder)
        {
            this.wr.Target = builder;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenUsingParameterAsTwoArguments()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        private readonly int a;
        private readonly int b;

        public Foo(int x)
            : this(x, x)
        {
        }

        public Foo(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
