namespace Gu.Analyzers.Test.GU0033DontIgnoreReturnValueOfTypeIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0033DontIgnoreReturnValueOfTypeIDisposable>
    {
        [Test]
        public async Task IgnoringFileOpenRead()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public void Meh()
    {
        ↓File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoringNewDisposable()
        {
            var disposableCode = @"
using System;
class Disposable : IDisposable
{
    public void Dispose()
	{
	}
}";

            var testCode = @"
public sealed class Foo
{
    public void Meh()
    {
        ↓new Disposable();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task FactoryMethodNewDisposable()
        {
            var disposableCode = @"
using System;

class Disposable : IDisposable
{
    public void Dispose()
	{
	}
}";

            var testCode = @"
public sealed class Foo
{
    public void Meh()
    {
        ↓Create();
    }

    private static Disposable Create()
    {
        return new Disposable();
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoringFileOpenReadPassedIntoCtor()
        {
            var testCode = @"
using System;
using System.IO;

public class Bar
{
    private readonly Stream stream;

    public Bar(Stream stream)
    {
       this.stream = stream;
    }
}

public sealed class Foo
{
    public Bar Meh()
    {
        return new Bar(↓File.OpenRead(string.Empty));
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoringNewDisposabledPassedIntoCtor()
        {
            var disposableCode = @"
using System;
class Disposable : IDisposable
{
    public void Dispose()
	{
	}
}";
            var barCode = @"
using System;

public class Bar
{
    private readonly IDisposable disposable;

    public Bar(IDisposable disposable)
    {
       this.disposable = disposable;
    }
}";

            var testCode = @"
public sealed class Foo
{
    public Bar Meh()
    {
        return new Bar(↓new Disposable());
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, barCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task Generic()
        {
            var interfaceCode = @"
    using System;
    public interface IDisposable<T> : IDisposable
    {
    }";

            var disposableCode = @"
    public sealed class Disposable<T> : IDisposable<T>
    {
        public void Dispose()
        {
        }
    }";

            var factoryCode = @"
    public class Factory
    {
        public static IDisposable<T> Create<T>() => new Disposable<T>();
    }";

            var testCode = @"
    using System.IO;

    public class Foo
    {
        public void Bar()
        {
            ↓Factory.Create<int>();
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { interfaceCode, disposableCode, factoryCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        [Explicit("Fix later.")]
        public async Task ConstrainedGeneric()
        {
            var factoryCode = @"
using System;

public class Factory
{
    public static T Create<T>() where T : IDisposable, new() => new T();
}";

            var disposableCode = @"
using System;

public sealed class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}";

            var testCode = @"
    public class Foo
    {
        public void Bar()
        {
            ↓Factory.Create<Disposable>();
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't ignore returnvalue of type IDisposable.");
            await this.VerifyCSharpDiagnosticAsync(new[] { factoryCode, disposableCode, testCode }, expected).ConfigureAwait(false);
        }
    }
}