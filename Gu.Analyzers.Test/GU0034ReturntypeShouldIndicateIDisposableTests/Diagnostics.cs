namespace Gu.Analyzers.Test.GU0034ReturntypeShouldIndicateIDisposableTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0034ReturntypeShouldIndicateIDisposable>
    {
        [Test]
        public async Task ReturnFileOpenReadAsObject()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public object Meh()
    {
        return ↓File.OpenRead(string.Empty);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Return type should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnStaticFieldPasswordBoxSecurePasswordAsObject()
        {
            var testCode = @"
using System.Windows.Controls;

public sealed class Foo
{
    private static readonly PasswordBox PasswordBox = new PasswordBox();

    public object Meh()
    {
        return ↓PasswordBox.SecurePassword;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Return type should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnFieldPasswordBoxSecurePasswordAsObject()
        {
            var testCode = @"
using System.Windows.Controls;

public sealed class Foo
{
    private readonly PasswordBox PasswordBox = new PasswordBox();

    public object Meh()
    {
        return ↓PasswordBox.SecurePassword;
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Return type should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task IndexerReturningObject()
        {
            var testCode = @"
using System.IO;

public class Foo
{
    public void Bar()
    {
        var meh = this[0];
    }

    public object this[int index]
    {
        get
        {
            return ↓File.OpenRead(string.Empty);
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Return type should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task ReturnFileOpenReadAsObjectExpressionBody()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public object Meh() => ↓File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Return type should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyReturnFileOpenReadAsObjectExpressionBody()
        {
            var testCode = @"
using System;
using System.IO;

public sealed class Foo
{
    public object Meh => ↓File.OpenRead(string.Empty);
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Return type should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task Lambda()
        {
            var testCode = @"
using System;
using System.IO;

internal static class Foo
{
    internal static void Bar()
    {
        Func<object> f = () =>
        {
	        var file = System.IO.File.OpenRead(null);
	        return ↓file;
        };
    }
}";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Return type should indicate that the value should be disposed.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}