namespace Gu.Analyzers.Test.CodeFixes.GenerateUselessDocsFixTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class CodeFixWhenMissingParameterDocsSA1611
    {
        internal class SpecialType
        {
            [Test]
            public void StandardTextForCancellationToken()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Threading;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        public void M(CancellationToken ↓cancellationToken)
        {
        }
    }
}";

                var fixedCode = @"
namespace RoslynSandbox
{
    using System.Threading;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""cancellationToken"">The <see cref=""CancellationToken""/> that cancels the operation.</param>
        public void M(CancellationToken cancellationToken)
        {
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, testCode, fixedCode, fixTitle: "Generate standard xml documentation for parameter.");
            }
        }
    }
}
