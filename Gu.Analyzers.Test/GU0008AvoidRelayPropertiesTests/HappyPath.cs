namespace Gu.Analyzers.Test.GU0008AvoidRelayPropertiesTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0008AvoidRelayProperties>
    {
        [TestCase("public int Value { get; set; }")]
        [TestCase("public int Value { get; } = 1;")]
        public async Task AutoProp(string property)
        {
            var fooCode = @"
public class Foo
{
    public int Value { get; set; }
}";

            fooCode = fooCode.AssertReplace("public int Value { get; set; }", property);
            await this.VerifyHappyPathAsync(fooCode)
                      .ConfigureAwait(false);
        }

        [TestCase("get { return this.value; }")]
        [TestCase("get { return value; }")]
        public async Task WithBackingField(string getter)
        {
            var fooCode = @"
public class Foo
{
    private int value;

    public int Value
    {
        get { return this.value; }
        set { this.value = value; }
    }
}";
            fooCode = fooCode.AssertReplace("get { return this.value; }", getter);
            await this.VerifyHappyPathAsync(fooCode)
                      .ConfigureAwait(false);
        }
    }
}