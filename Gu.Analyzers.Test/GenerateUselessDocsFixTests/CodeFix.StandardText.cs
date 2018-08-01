namespace Gu.Analyzers.Test.GenerateUselessDocsFixTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        [Test]
        public void StandardTextForCancellationToken()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Threading;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        public void Meh(↓CancellationToken cancellationToken)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Threading;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""cancellationToken"">The <see cref=""CancellationToken""/> that the task will observe.</param>
        public void Meh(CancellationToken cancellationToken)
        {
        }
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, testCode, fixedCode, fixTitle: "Generate standard xml documentation for parameter.");
        }
    }
}
