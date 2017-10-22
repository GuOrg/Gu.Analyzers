namespace Gu.Analyzers.Test.GU0022UseGetOnlyTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0022UseGetOnly>
    {
        public static readonly TestCase[] TestCases =
        {
            new TestCase("int", "A++;"),
            new TestCase("int", "A--;"),
            new TestCase("int", "A+=a;"),
            new TestCase("int", "A-=a;"),
            new TestCase("int", "A*=a;"),
            new TestCase("int", "A/=a;"),
            new TestCase("int", "A%=a;"),
            new TestCase("int", "A = a;"),
            new TestCase("bool", "A|=a;"),
        };

        [Test]
        public async Task DifferentProperties()
        {
            var interfaceCode = @"
namespace RoslynSandbox
{
    public interface IFoo
    {
        int D { get; set; }
    }
}";
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo : IFoo
    {
        // private int f;
        
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        int IFoo.D
        {
            get { return this.D; }
            set { this.D = value; }
        }

        public int E => A;

        // public int F
        // {
        //     get => this.f;
        //     set => this.f = value;
        // }
    }
}";
            await this.VerifyHappyPathAsync(interfaceCode, testCode)
                      .ConfigureAwait(false);
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task UpdatedInMethodThis(TestCase data)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int A { get; private set; }

        public void Update(int a)
        {
            this.A = a;
        }
    }
}";
            testCode = testCode.AssertReplace("A = a;", data.Update)
                               .AssertReplace("int", data.Type);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task UpdatedInMethodUnderscoreNames(TestCase data)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public int A { get; private set; }

        public void Update(int a)
        {
            A = a;
        }
    }
}";
            testCode = testCode.AssertReplace("A = a;", data.Update)
                               .AssertReplace("int", data.Type);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UpdatedInLambdaInCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            this.E += (_, __) => this.A = 5;
        }

        public event EventHandler E;

        public int A { get; private set; }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task UpdatingOtherInstanceInCtor(TestCase data)
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(Foo previous, int a)
        {
            previous.A = a;
        }

        public int A { get; private set; }
    }
}";
            testCode = testCode.AssertReplace("A = a;", data.Update)
                               .AssertReplace("int", data.Type);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        internal class TestCase
        {
            public TestCase(string type, string update)
            {
                this.Type = type;
                this.Update = update;
            }

            public string Type { get; }

            public string Update { get; }

            public override string ToString() => this.Update;
        }
    }
}