namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : DiagnosticVerifier<GU0008AvoidRelayProperties>
    {
        [TestCase("return this.bar.Value;")]
        [TestCase("return bar.Value;")]
        public async Task WhenReturningPropertyOfField(string getter)
        {
            var fooCode = @"
public class Foo
{
    private readonly Bar bar;

    public Foo()
    {
        this.bar = new Bar();
    }

    ↓public int Value
    { 
        get
        {
            return this.bar.Value;
        }
    }
}";
            var barCode = @"
public class Bar
{
    public int Value { get; }
}";
            fooCode = fooCode.AssertReplace("return this.bar.Value;", getter);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref fooCode)
                               .WithMessage("Avoid relay properties.");
            await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenReturningPropertyOfFieldExpressionBody()
        {
            var fooCode = @"
public class Foo
{
    private readonly Bar bar;

    public Foo()
    {
        this.bar = new Bar();
    }

    ↓public int Value => this.bar.Value;
}";
            var barCode = @"
public class Bar
{
    public int Value { get; }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref fooCode)
                               .WithMessage("Avoid relay properties.");
            await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode }, expected).ConfigureAwait(false);
        }
    }
}
