namespace Gu.Analyzers.Test.GU0050IgnoreEventsWhenSerializingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly GU0050IgnoreEventsWhenSerializing Analyzer = new GU0050IgnoreEventsWhenSerializing();
        private static readonly AddNonSerializedFixProvider Fix = new AddNonSerializedFixProvider();
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

            var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated("GU0050", "Ignore events when serializing.", testCode, out testCode);
            AnalyzerAssert.Diagnostics(Analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void NotIgnoredEvent()
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
        }

        [Test]
        public void NotIgnoredEventWithAttribute()
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
        }

        [Test]
        public void NotIgnoredEventHandler()
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
        }
    }
}
