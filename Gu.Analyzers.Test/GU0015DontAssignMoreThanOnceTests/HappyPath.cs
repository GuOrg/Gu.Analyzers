namespace Gu.Analyzers.Test.GU0015DontAssignMoreThanOnceTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly DiagnosticAnalyzer Analyzer = new SimpleAssignmentAnalyzer();

        [Test]
        public void SimpleAssign()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private readonly int value;

        public Foo(int value)
        {
            this.value = value;
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void AssignInIfElse()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value;

        public Foo(int value)
        {
            if (value < 0)
            {
                this.value = 0;
            }
            else
            {
                this.value = value;
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreMutableInLambda()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(int value)
        {
            this.Value = value;
            System.Console.CancelKeyPress += (_, __) => Console.WriteLine(this.Value);
        }

        public int Value { get; set; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreOutParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static bool TryGet(int i, out string text)
        {
            text = null;
            if (i > 10)
            {
                text = i.ToString();
            }

            return text != null;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void IgnoreObjectInitializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo()
        {
            var bar1 = new Bar { Value = 1 };
            var bar2 = new Bar { Value = 1 };
        }
    }

    public class Bar
    {
        public int Value { get; set; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DifferentInstances()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(Bar bar1, Bar bar2)
        {
            bar1.Value = 1;
            bar2.Value = 3;
        }
    }

    public class Bar
    {
        public int Value { get; set; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void Enum()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Run()
        {
            this.Status = Status.Running;
            _ = Console.ReadKey();
            this.Status = Status.Finished;
        }

        public Status Status { get; private set; }
    }

    enum Status
    {
        Running,
        Finished,
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void Boolean()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public void Run()
        {
            this.Running = true;
            _ = Console.ReadKey();
            this.Running = false;
        }

        public bool Running { get; private set; }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
