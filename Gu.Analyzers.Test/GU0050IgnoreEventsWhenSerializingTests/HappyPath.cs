namespace Gu.Analyzers.Test.GU0050IgnoreEventsWhenSerializingTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0050IgnoreEventsWhenSerializing>
    {
        [Test]
        public async Task IgnoredEvent()
        {
            var testCode = @"
using System;

[Serializable]
public class Foo
{
    public Foo(int a, int b, int c, int d)
    {
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
    }

    [field:NonSerialized]
    public event EventHandler SomeEvent;

    public int A { get; }

    public int B { get; protected set;}

    public int C { get; internal set; }

    public int D { get; set; }

    public int E => A;
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoredEventSimple()
        {
            var testCode = @"
using System;

[Serializable]
public class Foo
{
    [field:NonSerialized]
    public event EventHandler SomeEvent;
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoredEventHandler()
        {
            var testCode = @"
using System;

[Serializable]
public class Foo
{
    [NonSerialized]
    private EventHandler someEvent;

    public event EventHandler SomeEvent
    {
        add { this.someEvent += value; }
        remove { this.someEvent -= value; }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NotSerializable()
        {
            var testCode = @"
using System;

public class Foo
{
    public Foo(int a, int b, int c, int d)
    {
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
    }

    public event EventHandler SomeEvent;

    public int A { get; }

    public int B { get; protected set;}

    public int C { get; internal set; }

    public int D { get; set; }

    public int E => A;
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}