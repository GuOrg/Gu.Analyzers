namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0011DontIgnoreReturnValue>
    {
        [TestCase("ints.Select(x => x);")]
        [TestCase("ints.Select(x => x).Where(x => x > 1);")]
        [TestCase("ints.Where(x => x > 1);")]
        public async Task Linq(string linq)
        {
            var testCode = @"
using System.Linq;
class Foo
{
    void Bar()
    {
        var ints = new[] { 1, 2, 3 };
        ↓ints.Select(x => x);
    }
}";
            testCode = testCode.AssertReplace("ints.Select(x => x);", linq);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task StringBuilderWriteLine()
        {
            var testCode = @"
using System.Text;
public class Foo
{
    private int value;

    public void Bar()
    {
        var sb = new StringBuilder();
        ↓sb.AppendLine(""test"").ToString();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected).ConfigureAwait(false);
        }
    }
}
