namespace Gu.Analyzers.Test.GU0050IgnoreEventsWhenSerializingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0050IgnoreEventsWhenSerializing Analyzer = new();

        [Test]
        public static void IgnoredEvent()
        {
            var code = @"
namespace N
{
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
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoredEventSimple()
        {
            var code = @"
namespace N
{
    using System;

    [Serializable]
    public class C
    {
        [field:NonSerialized]
        public event EventHandler E;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoredEventHandler()
        {
            var code = @"
namespace N
{
    using System;

    [Serializable]
    public class C
    {
        [NonSerialized]
        private EventHandler e;

        public event EventHandler E
        {
            add { this.e += value; }
            remove { this.e -= value; }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void NotSerializable()
        {
            var code = @"
namespace N
{
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
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
