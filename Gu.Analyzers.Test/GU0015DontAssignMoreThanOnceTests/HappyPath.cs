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

        [Test]
        public void WhenUsingTheValueStringReplace()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static string Bar(string text)
        {
            text = text.Replace("" "", ""_"");
            text = text.Replace("":"", ""_"");
            return text;
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenUsingTheValue1()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = this.Value1 * 397;
                hash = hash ^ this.Value2;
                return hash;
            }
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenUsingTheValue2()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int value1, int value2)
        {
            this.Value1 = value1;
            this.Value2 = value2;
        }

        public int Value1 { get; }

        public int Value2 { get; }

        public static bool operator ==(Foo left, Foo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Foo left, Foo right)
        {
            return !Equals(left, right);
        }

        protected bool Equals(Foo other)
        {
            return this.Value1 == other.Value1 && this.Value2 == other.Value2;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((Foo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = this.Value1 * 397;
                hash = hash ^ this.Value2;
                return hash;
            }
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
