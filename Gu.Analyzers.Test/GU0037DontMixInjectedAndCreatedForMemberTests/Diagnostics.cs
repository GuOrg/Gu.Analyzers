namespace Gu.Analyzers.Test.GU0037DontMixInjectedAndCreatedForMemberTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0037DontMixInjectedAndCreatedForMember>
    {
        [TestCase("stream ?? File.OpenRead(string.Empty)")]
        [TestCase("Stream ?? File.OpenRead(string.Empty)")]
        [TestCase("File.OpenRead(string.Empty) ?? stream")]
        [TestCase("File.OpenRead(string.Empty) ?? Stream")]
        [TestCase("true ? stream : File.OpenRead(string.Empty)")]
        [TestCase("true ? Stream : File.OpenRead(string.Empty)")]
        [TestCase("true ? File.OpenRead(string.Empty) : stream")]
        [TestCase("true ? File.OpenRead(string.Empty) : Stream")]
        public async Task InjectedAndCreatedField(string code)
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    private static readonly Stream Stream = File.OpenRead(string.Empty);
    ↓private readonly Stream stream;

    public Foo(Stream stream)
    {
        this.stream = stream ?? File.OpenRead(string.Empty);
    }
}";
            testCode = testCode.AssertReplace("stream ?? File.OpenRead(string.Empty)", code);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedAndCreatedFieldCtorAndInitializer()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    ↓private readonly Stream stream = File.OpenRead(string.Empty);

    public Foo(Stream stream)
    {
        this.stream = stream;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedAndCreatedFieldTwoCtors()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    ↓private readonly Stream stream;

    public Foo()
    {
        this.stream = File.OpenRead(string.Empty);
    }

    public Foo(Stream stream)
    {
        this.stream = stream;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [TestCase("public Stream Stream { get; }")]
        [TestCase("public Stream Stream { get; private set; }")]
        [TestCase("public Stream Stream { get; protected set; }")]
        [TestCase("public Stream Stream { get; set; }")]
        public async Task InjectedAndCreatedProperty(string property)
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    public Foo(Stream stream)
    {
        this.Stream = stream;
    }

    ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
}";
            testCode = testCode.AssertReplace("public Stream Stream { get; }", property);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task InjectedAndCreatedPropertyTwoCtors()
        {
            var testCode = @"
using System.IO;

public sealed class Foo
{
    public Foo()
    {
        this.Stream = File.OpenRead(string.Empty);
    }

    public Foo(Stream stream)
    {
        this.Stream = stream;
    }

    ↓public Stream Stream { get; }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ProtectedMutableField()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓protected Stream stream = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ProtectedMutableProperty()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    ↓public Stream Stream { get; protected set; } = File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Don't assign member with injected and created disposables.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}