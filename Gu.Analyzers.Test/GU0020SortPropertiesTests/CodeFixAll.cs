namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class CodeFixAll : CodeFixVerifier<GU0020SortProperties, SortPropertiesCodeFixProvider>
    {
        [Test]
        public async Task WhenMutableBeforeGetOnlyFirst()
        {
            var testCode = @"
public class Foo
{
    public int A { get; set; }

    public int B { get; }

    public int C { get; }

    public int D { get; }
}";

            var fixedCode = @"
public class Foo
{
    public int B { get; }

    public int C { get; }

    public int D { get; }

    public int A { get; set; }
}";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenAMess1()
        {
            var testCode = @"
public class Foo
{
    public int A { get; set; }

    public int B { get; private set; }

    public int C { get; }

    public int D => C;
}";

            var fixedCode = @"
public class Foo
{
    public int C { get; }

    public int D => C;

    public int B { get; private set; }

    public int A { get; set; }
}";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenAMess1WithDocs()
        {
            var testCode = @"
public class Foo
{
    /// <summary>
    /// Docs for A
    /// </summary>
    public int A { get; set; }

    /// <summary>
    /// Docs for B
    /// </summary>
    public int B { get; private set; }

    /// <summary>
    /// Docs for C
    /// </summary>
    public int C { get; }

    /// <summary>
    /// Docs for D
    /// </summary>
    public int D => C;
}";

            var fixedCode = @"
public class Foo
{
    /// <summary>
    /// Docs for C
    /// </summary>
    public int C { get; }

    /// <summary>
    /// Docs for D
    /// </summary>
    public int D => C;

    /// <summary>
    /// Docs for B
    /// </summary>
    public int B { get; private set; }

    /// <summary>
    /// Docs for A
    /// </summary>
    public int A { get; set; }
}";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task PreservesDocumentOrder()
        {
            var testCode = @"
public class Foo
{
    public int A { get; set; }

    public int B { get; private set; }

    public int C { get; set; }

    public int D { get; private set; }
}";

            var fixedCode = @"
public class Foo
{
    public int B { get; private set; }

    public int D { get; private set; }

    public int A { get; set; }

    public int C { get; set; }
}";
            await this.VerifyCSharpFixAllFixAsync(testCode, fixedCode)
                      .ConfigureAwait(false);
        }
    }
}