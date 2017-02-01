namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : DiagnosticVerifier<GU0008AvoidRelayProperties>
    {
        [TestCase("return this.bar.Value;")]
        [TestCase("return bar.Value;")]
        public async Task WhenReturningPropertyOfInjectedField(string getter)
        {
            var fooCode = @"
public class Foo
{
    private readonly Bar bar;

    public Foo(Bar bar)
    {
        this.bar = bar;
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

        [TestCase("this.bar.Value;")]
        [TestCase("bar.Value;")]
        public async Task WhenReturningPropertyOfFieldExpressionBody(string body)
        {
            var fooCode = @"
public class Foo
{
    private readonly Bar bar;

    public Foo(Bar bar)
    {
        this.bar = bar;
    }

    ↓public int Value => this.bar.Value;
}";
            var barCode = @"
public class Bar
{
    public int Value { get; }
}";
            fooCode = fooCode.AssertReplace("this.bar.Value;", body);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref fooCode)
                               .WithMessage("Avoid relay properties.");
            await this.VerifyCSharpDiagnosticAsync(new[] { fooCode, barCode }, expected).ConfigureAwait(false);
        }
    }
}
