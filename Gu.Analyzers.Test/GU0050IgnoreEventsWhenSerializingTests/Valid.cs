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
    public class C
    {
        public C(int p1, int p2, int p3, int p4)
        {
            this.P1 = p1;
            this.P2 = p2;
            this.P3 = p3;
            this.P4 = p4;
        }

        [field:NonSerialized]
        public event EventHandler? E;

        public int P1 { get; }

        public int P2 { get; protected set;}

        public int P3 { get; internal set; }

        public int P4 { get; set; }

        public int P5 => P1;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
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
        public event EventHandler? E;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
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
        private EventHandler? e;

        public event EventHandler? E
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

    public class C
    {
        public C(int p1, int p2, int p3, int p4)
        {
            this.P1 = p1;
            this.P2 = p2;
            this.P3 = p3;
            this.P4 = p4;
        }

        [field:NonSerialized]
        public event EventHandler? E;

        public int P1 { get; }

        public int P2 { get; protected set;}

        public int P3 { get; internal set; }

        public int P4 { get; set; }

        public int P5 => P1;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
