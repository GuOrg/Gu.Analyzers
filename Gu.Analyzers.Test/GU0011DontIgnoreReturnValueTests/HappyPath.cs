namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0011DontIgnoreReturnValue>
    {
        [Test]
        public async Task RealisticClass()
        {
            var testCode = @"
public class Foo
{
    public int Value { get; set; }
    
    private void Bar()
    {
        Meh();
    }

    private void Meh()
    {
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task VoidMethod()
        {
            var testCode = @"
public class Foo
{
    private void Bar()
    {
        Meh();
    }

    private void Meh()
    {
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IfTry()
        {
            var testCode = @"
public class Foo
{
    private void Bar()
    {
        int value;
        if(Try(out value))
        {
        }
    }

    private bool Try(out int value)
    {
        value = 1;
        return true;
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StringBuilderAppendLine()
        {
            var testCode = @"
using System.Text;
public class Foo
{
    public void Bar()
    {
        var sb = new StringBuilder();
        sb.AppendLine(""test"");
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenReturningThis()
        {
            var testCode = @"
public class Foo
{
    public Foo Bar()
    {
        return this;
    }

    public void Meh()
    {
        Bar();
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}