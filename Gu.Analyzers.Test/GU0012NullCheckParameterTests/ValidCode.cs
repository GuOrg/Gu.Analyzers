namespace Gu.Analyzers.Test.GU0012NullCheckParameterTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    [TestFixture(typeof(SimpleAssignmentAnalyzer))]
    [TestFixture(typeof(ParameterAnalyzer))]
    internal class ValidCode<T>
        where T : DiagnosticAnalyzer, new()
    {
        private static readonly T Analyzer = new T();

        [Test]
        public void WhenPrivate()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        private Foo(string text)
        {
            this.text = text;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenDefaultValue()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text = null)
        {
            this.text = text;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenValueType()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly int i;

        public Foo(int i)
        {
            this.i = i;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenThrowing()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenThrowingOnLineAbove()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
#pragma warning disable GU0015 // Don't assign same more than once.
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.text = text;
#pragma warning restore GU0015 // Don't assign same more than once.
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("text == null")]
        [TestCase("text is null")]
        public void WhenOldStyleNullCheckAbove(string check)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly string text;

        public Foo(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            this.text = text;
        }
    }
}".AssertReplace("text == null", check);

            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
