namespace Gu.Analyzers.Test.GU0030UseUsingTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        public class Recursion : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task IgnoresRecursiveProperty()
            {
                var testCode = @"
using System;

public class Foo
{
    public IDisposable RecursiveProperty => RecursiveProperty;

    public void Meh()
    {
        var item = RecursiveProperty;

        using(var meh = RecursiveProperty)
        {
        }

        using(RecursiveProperty)
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }

            [Test]
            public async Task IgnoresRecursiveMethod()
            {
                var testCode = @"
using System;

public class Foo
{
    public IDisposable RecursiveMethod() => RecursiveMethod();

    public void Meh()
    {
        var meh = RecursiveMethod();

        using(var item = RecursiveMethod())
        {
        }

        using(RecursiveMethod())
        {
        }
    }
}";
                await this.VerifyHappyPathAsync(testCode)
                          .ConfigureAwait(false);
            }
        }
    }
}