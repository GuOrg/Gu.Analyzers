namespace Gu.Analyzers.Test.GU0050IgnoreEventsWhenSerializingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly GU0050IgnoreEventsWhenSerializing Analyzer = new();
        private static readonly NonSerializedFix Fix = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0050IgnoreEventsWhenSerializing);

        [Test]
        public static void Messages()
        {
            var before = @"
namespace N
{
    using System;

    [Serializable]
    public class C
    {
        ↓public event EventHandler? E;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
    }
}";

            var after = @"
namespace N
{
    using System;

    [Serializable]
    public class C
    {
        [field: NonSerialized]
        public event EventHandler? E;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
    }
}";
            var expectedDiagnostic = ExpectedDiagnostic.WithMessage("Ignore events when serializing");
            RoslynAssert.CodeFix(Analyzer, Fix, expectedDiagnostic, before, after, fixTitle: "[field:NonSerialized].");
        }

        [Test]
        public static void Event()
        {
            var before = @"
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

        ↓public event EventHandler? E;

        public int P1 { get; }

        public int P2 { get; protected set;}

        public int P3 { get; internal set; }

        public int P4 { get; set; }

        public int P5 => P1;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
    }
}";

            var after = @"
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

        [field: NonSerialized]
        public event EventHandler? E;

        public int P1 { get; }

        public int P2 { get; protected set;}

        public int P3 { get; internal set; }

        public int P4 { get; set; }

        public int P5 => P1;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void EventWithAttribute()
        {
            var attributeCode = @"
namespace N
{
    using System;

    class SomeAttribute : Attribute
    {
    }
}";
            var before = @"
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

        ↓[Some]
        public event EventHandler? E;

        public int P1 { get; }

        public int P2 { get; protected set;}

        public int P3 { get; internal set; }

        public int P4 { get; set; }

        public int P5 => P1;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
    }
}";

            var after = @"
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

        [Some]
        [field: NonSerialized]
        public event EventHandler? E;

        public int P1 { get; }

        public int P2 { get; protected set;}

        public int P3 { get; internal set; }

        public int P4 { get; set; }

        public int P5 => P1;

        public void M() =>  this.E?.Invoke(this, EventArgs.Empty);
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { attributeCode, before }, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { attributeCode, before }, after);
        }

        [Test]
        public static void EventHandler()
        {
            var before = @"
namespace N
{
    using System;

    [Serializable]
    public class C
    {
        ↓private EventHandler? e;

        public event EventHandler? E
        {
            add { this.e += value; }
            remove { this.e -= value; }
        }
    }
}";

            var after = @"
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
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void TwoEvents()
        {
            var before = @"
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

        ↓public event EventHandler? E1;

        ↓public event EventHandler? E2;

        public int P1 { get; }

        public int P2 { get; protected set;}

        public int P3 { get; internal set; }

        public int P4 { get; set; }

        public int P5 => P1;

        public void M1() =>  this.E1?.Invoke(this, EventArgs.Empty);

        public void M2() =>  this.E2?.Invoke(this, EventArgs.Empty);
    }
}";

            var after = @"
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

        [field: NonSerialized]
        public event EventHandler? E1;

        [field: NonSerialized]
        public event EventHandler? E2;

        public int P1 { get; }

        public int P2 { get; protected set;}

        public int P3 { get; internal set; }

        public int P4 { get; set; }

        public int P5 => P1;

        public void M1() =>  this.E1?.Invoke(this, EventArgs.Empty);

        public void M2() =>  this.E2?.Invoke(this, EventArgs.Empty);
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
