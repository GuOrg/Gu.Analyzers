namespace Gu.Analyzers.Test.GU0002NamedArgumentPositionMatchesTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0002NamedArgumentPositionMatches>
    {
        [TestCase("new Foo(a, b)")]
        [TestCase("new Foo(a: a, b: b)")]
        public async Task ConstructorCallWithTwoArguments(string call)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }

        private Foo Create(int a, int b)
        {
            return new Foo(a, b);
        }
    }
}";
            testCode = testCode.AssertReplace("new Foo(a, b)", call);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("new Foo(a, b)")]
        [TestCase("new Foo(a: a, b: b)")]
        public async Task ConstructorCallWithTwoArgumentsStruct(string call)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public struct Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }

        private Foo Create(int a, int b)
        {
            return new Foo(a, b);
        }
    }
}";
            testCode = testCode.AssertReplace("new Foo(a, b)", call);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorCallWithNamedArgumentsOnSameRow()
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

        private Foo Create(int a, int b, int c, int d)
        {
            return new Foo(a: a, b: b, c: c, d: d);
        }
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorCallWithArgumentsOnSameRow()
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

        private Foo Create(int a, int b, int c, int d)
        {
            return new Foo(a, b, c, d);
        }
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorCallWithNamedArgumentsOnSeparateRows()
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

        private Foo Create(int a, int b, int c, int d)
        {
            return new Foo(
                a: a, 
                b: b, 
                c: c, 
                d: d);
        }
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresStringFormat()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Globalization;

    public static class Foo
    {
        private static string Bar(int a, int b, int c, int d)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                ""{0}{1}{2}{3}"",
                a,
                b,
                c,
                d);
        }
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhendifferentTypes()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, double b, string c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public double B { get; }

        public string C { get; }

        public int D { get; }

        private Foo Create(int a, double b, string c, int d)
        {
            return new Foo(
                a, 
                b, 
                c, 
                d);
        }
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenInExpressionTree()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Linq.Expressions;

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

        private Expression<Func<Foo>> Create(int a, int b, int c, int d)
        {
            return () => new Foo(
                a,
                b,
                c,
                d);
        }
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}