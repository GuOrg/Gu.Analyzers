namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<Analyzers.GU0009UseNamedParametersForBooleans>
    {
        [Test]
        public async Task UsesNamedParameter()
        {
            var testCode = @"
using System;
using System.Collections.Generic;

public class Foo
{
    public void Floof(int howMuch, bool useFluffyBuns)
    {
        
    }

    public void Another()
    {
        Floof(42, useFluffyBuns: false);
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingADefaultBooleanParameter()
        {
            var testCode = @"
using System;
using System.Collections.Generic;

public class Foo
{
    public void Floof(int howMuch, bool useFluffyBuns = true)
    {
        
    }

    public void Another()
    {
        Floof(42);
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NondeducedGenericBooleanParameter()
        {
            var testCode = @"
using System;
using System.Collections.Generic;

public class Foo
{
    public void Another()
    {
        var a = new List<bool>();
        a.Add(true);
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DeducedGenericBooleanParameter()
        {
            var testCode = @"
using System;
using System.Collections.Generic;

public class Foo
{
    public void Another()
    {
        var a = Tuple.Create(true, false);
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task FunctionAcceptingABooleanVariable()
        {
            var testCode = @"
using System;
using System.Collections.Generic;

public class Foo
{
    public void Floof(int howMuch, bool useFluffyBuns)
    {
        
    }

    public void Another(bool useFluffyBuns)
    {
        Floof(42, useFluffyBuns);
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task BooleanParamsArray()
        {
            var testCode = @"
using System;
using System.Collections.Generic;

public class Foo
{
    public void Floof(params bool[] useFluffyBuns)
    {
        
    }

    public void Another()
    {
        Floof(true, true, true, false, true, false);
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontWarnOnDisposePattern()
        {
            var testCode = @"
using System;
using System.IO;

public abstract class FooBase : IDisposable
{
    private readonly Stream stream = File.OpenRead(string.Empty);
    private bool disposed = false;

    public void Dispose()
    {
        this.Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        if (disposing)
        {
            this.stream.Dispose();
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontWarnOnAssertAreEqual()
        {
            var testCode = @"
using NUnit.Framework;

public class FooTests
{
    [Test]
    public void Bar()
    {
        Assert.AreEqual(true, true);
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}