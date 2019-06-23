namespace Gu.Analyzers.Test.GU0016PreferLambdaTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new MethodGroupAnalyzer();
        private static readonly CodeFixProvider Fix = new UseLambdaFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0016");

        [Test]
        public void LinqWhereStaticMethod()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.Linq;

    public class Foo
    {
        public Foo(IEnumerable<int> ints)
        {
            var meh = ints.Where(↓IsEven);
        }

        private static bool IsEven(int x) => x % 2 == 0;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.Linq;

    public class Foo
    {
        public Foo(IEnumerable<int> ints)
        {
            var meh = ints.Where(x => IsEven(x));
        }

        private static bool IsEven(int x) => x % 2 == 0;
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void LinqWhereStaticMethodWhenNameCollision()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.Linq;

    public class Foo
    {
        public Foo(IEnumerable<int> ints, int x)
        {
            var meh = ints.Where(↓IsEven);
        }

        private static bool IsEven(int x) => x % 2 == 0;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;
    using System.Linq;

    public class Foo
    {
        public Foo(IEnumerable<int> ints, int x)
        {
            var meh = ints.Where(x_ => IsEven(x_));
        }

        private static bool IsEven(int x) => x % 2 == 0;
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }

        [Test]
        public void EventHandler()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class Foo
    {
        public Foo()
        {
            Console.CancelKeyPress += ↓OnCancelKeyPress;
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    internal class Foo
    {
        public Foo()
        {
            Console.CancelKeyPress += (sender, e) => OnCancelKeyPress(sender, e);
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, testCode, fixedCode);
        }
    }
}
