namespace Gu.Analyzers.Test.GU0032DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class HappyPath
    {
        internal class RefAndOut : NestedHappyPathVerifier<HappyPath>
        {
            [Test]
            public async Task AssigningVariableViaOutParameter()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    public bool Update()
    {
        Stream stream;
        return TryGetStream(out stream);
    }

    public bool TryGetStream(out Stream stream)
    {
        stream = File.OpenRead(string.Empty);
        return true;
    }
}";
                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningFieldWithCahcedViaOutParameter()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public bool Update()
    {
        return TryGetStream(out stream);
    }

    public bool TryGetStream(out Stream result)
    {
        result = this.stream;
        return true;
    }
}";

                await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
            }
        }
    }
}