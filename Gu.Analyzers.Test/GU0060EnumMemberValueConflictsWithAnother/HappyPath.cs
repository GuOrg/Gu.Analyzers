namespace Gu.Analyzers.Test.GU0060EnumMemberValueConflictsWithAnother
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<Analyzers.GU0060EnumMemberValueConflictsWithAnother>
    {
        [Test]
        public async Task ExplicitAlias()
        {
            var testCode = @"
using System;

[Flags]
public enum Good
{
    A = 1,
    B = 2,
    Gooooood = B
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExplicitBitwiseOrSum()
        {
            var testCode = @"
using System;

[Flags]
public enum Good
{
    A = 1,
    B = 2,
    Gooooood = A | B
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task SequentialNonFlagEnum()
        {
            var testCode = @"
using System;

public enum Bad
{
    None,
    A,
    B,
    Baaaaaaad
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}