namespace Gu.Analyzers.Test.GU0050IgnoreEventsWhenSerializingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly GU0050IgnoreEventsWhenSerializing Analyzer = new GU0050IgnoreEventsWhenSerializing();
        private static readonly NonSerializedFix Fix = new NonSerializedFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0050");

        [Test]
        public void Message()
        {
            var testCode = @"
namespace RoslynSandbox
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

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Ignore events when serializing."), testCode);
        }

        [Test]
        public void Event()
        {
            var testCode = @"
namespace RoslynSandbox
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

            var fixedCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void EventWithAttribute()
        {
            var attributeCode = @"
namespace RoslynSandbox
{
    using System;

    class BarAttribute : Attribute
    {
    }
}";
            var testCode = @"
namespace RoslynSandbox
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

            var fixedCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { attributeCode, testCode }, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { attributeCode, testCode }, fixedCode);
        }

        [Test]
        public void EventHandler()
        {
            var testCode = @"
namespace RoslynSandbox
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

            var fixedCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void TwoEvents()
        {
            var testCode = @"
namespace RoslynSandbox
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

            var fixedCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }
    }
}
