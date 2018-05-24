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
    }
}
