namespace Gu.Analyzers.Test.GU0022UseGetOnlyTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0022UseGetOnly>
    {
        public static readonly UpdateItem[] UpdateSource =
        {
            new UpdateItem("int", "this.A++;"),
            new UpdateItem("int", "this.A--;"),
            new UpdateItem("int", "this.A+=a;"),
            new UpdateItem("int", "this.A-=a;"),
            new UpdateItem("int", "this.A*=a;"),
            new UpdateItem("int", "this.A/=a;"),
            new UpdateItem("int", "this.A%=a;"),
            new UpdateItem("int", "this.A = a;"),
            new UpdateItem("bool", "this.A|=a;"),
        };

        [Test]
        public async Task MiscProperties()
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

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        public int E => A;
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCaseSource(nameof(UpdateSource))]
        public async Task UpdatedInMethod(UpdateItem data)
        {
            var testCode = @"
public class Foo
{
    public int A { get; private set; }

    public void Update(int a)
    {
        this.A = a;
    }
}";
            testCode = testCode.AssertReplace("this.A = a;", data.Update)
                               .AssertReplace("int", data.Type);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UpdatedInLambdaInCtor()
        {
            var testCode = @"
using System;

public class Foo
{
    public Foo()
    {
        this.E += (_, __) => this.A = 5;
    }

    public event EventHandler E;

    public int A { get; private set; }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        internal class UpdateItem
        {
            public UpdateItem(string type, string update)
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