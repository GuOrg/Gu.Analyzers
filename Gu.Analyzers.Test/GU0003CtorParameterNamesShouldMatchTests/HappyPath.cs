namespace Gu.Analyzers.Test.GU0003CtorParameterNamesShouldMatchTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0003CtorParameterNamesShouldMatch>
    {
        [Test]
        public async Task ConstructorSettingProperties()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ChainedConstructorSettingProperties()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task BaseConstructorCall()
        {
            var fooCode = @"
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }";
            var barCode = @"
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
    }";
            await this.VerifyHappyPathAsync(fooCode, barCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingField()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingFieldPrefixedByUnderscore()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenSettingTwoProperties()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
            this.B = a;
        }

        public int A { get; }

        public int B { get; }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenBaseIsParams()
        {
            var fooCode = @"
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }";
            var barCode = @"
    public class Foo
    {
        public Foo(params int[] values)
        {
            this.Values = values;
        }

        public int[] Values { get; }
    }";
            await this.VerifyHappyPathAsync(fooCode, barCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenBaseIsParams2()
        {
            var fooCode = @"
    public class Bar : Foo
    {
        public Bar(int a, int b, int c, int d)
            : base(a, b, c, d)
        {
        }
    }";
            var barCode = @"
    public class Foo
    {
        public Foo(int a, params int[] values)
        {
            this.A = a;
            this.Values = values;
        }

        public int A { get; }

        public int[] Values { get; }
    }";
            await this.VerifyHappyPathAsync(fooCode, barCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresIdCaps()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int id)
        {
            this.ID = id;
        }

        public int ID { get; }
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }
    }
}