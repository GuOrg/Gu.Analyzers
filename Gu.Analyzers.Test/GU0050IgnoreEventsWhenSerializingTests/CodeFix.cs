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
    public class Foo
    {
        ↓public event EventHandler SomeEvent;
    }
}";

            var after = @"
namespace N
{
    using System;

    [Serializable]
    public class Foo
    {
        [field: NonSerialized]
        public event EventHandler SomeEvent;
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
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        ↓public event EventHandler SomeEvent;

        public int A { get; }

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        public int E => A;
    }
}";

            var after = @"
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

        [field: NonSerialized]
        public event EventHandler SomeEvent;

        public int A { get; }

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        public int E => A;
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

    class BarAttribute : Attribute
    {
    }
}";
            var before = @"
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

        ↓[Bar]
        public event EventHandler SomeEvent;

        public int A { get; }

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        public int E => A;
    }
}";

            var after = @"
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

        [Bar]
        [field: NonSerialized]
        public event EventHandler SomeEvent;

        public int A { get; }

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        public int E => A;
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
    public class Foo
    {
        ↓private EventHandler someEvent;

        public event EventHandler SomeEvent
        {
            add { this.someEvent += value; }
            remove { this.someEvent -= value; }
        }
    }
}";

            var after = @"
namespace N
{
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
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        ↓public event EventHandler Event1;

        ↓public event EventHandler Event2;

        public int A { get; }

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        public int E => A;
    }
}";

            var after = @"
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

        [field: NonSerialized]
        public event EventHandler Event1;

        [field: NonSerialized]
        public event EventHandler Event2;

        public int A { get; }

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        public int E => A;
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
