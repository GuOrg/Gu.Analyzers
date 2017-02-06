namespace Gu.Analyzers.Test.GU0030UseUsingTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0030UseUsing>
    {
        [Test]
        public async Task PasswordBoxSecurePassword()
        {
            var testCode = @"
    using System.Windows.Controls;

    public class Foo
    {
        public PasswordBox PasswordBox { get; }

        public long Bar()
        {
            ↓var pwd = PasswordBox.SecurePassword;
            return pwd.Length;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task FileOpenRead()
        {
            var testCode = @"
    using System.IO;

    public class Foo
    {
        public long Bar()
        {
            ↓var stream = File.OpenRead(string.Empty);
            return stream.Length;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task NewDisposable()
        {
            var disposableCode = @"
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }";

            var testCode = @"
    public static class Foo
    {
        public static long Bar()
        {
            ↓var meh = new Disposable();
            return 1;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode, disposableCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task MethodCreatingDisposable1()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream()
        {
            return File.OpenRead(string.Empty);
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task MethodCreatingDisposable2()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream()
        {
            var stream = File.OpenRead(string.Empty);
            return stream;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task MethodCreatingDisposableExpressionBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static long Bar()
        {
            ↓var stream = GetStream();
            return stream.Length;
        }

        public static Stream GetStream() => File.OpenRead(string.Empty);
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyCreatingDisposableSimple()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Stream 
        {
           get { return File.OpenRead(string.Empty); }
        }

        public static long Bar()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyCreatingDisposableGetBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Stream 
        {
           get
           {
               var stream = File.OpenRead(string.Empty);
               return stream;
           }
        }

        public static long Bar()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task PropertyCreatingDisposableExpressionBody()
        {
            var testCode = @"
    using System.IO;

    public static class Foo
    {
        public static Stream Stream => File.OpenRead(string.Empty);

        public static long Bar()
        {
            ↓var stream = Stream;
            return stream.Length;
        }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task AwaitCreateAsync()
        {
            var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
	{
	}
}";

            var testCode = @"
using System;
using System.Threading.Tasks;

internal static class Foo
{
    internal static async Task Bar()
    {
        ↓var stream = await CreateAsync();
    }

    internal static async Task<IDisposable> CreateAsync()
    {
        return new Disposable();
    }
}";
            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task AwaitCreateAsyncTaskFromResult()
        {
            var disposableCode = @"
using System;

public class Disposable : IDisposable
{
    public void Dispose()
	{
	}
}";

            var testCode = @"
using System;
using System.Threading.Tasks;

internal static class Foo
{
    internal static async Task Bar()
    {
        ↓var stream = await CreateAsync();
    }

    internal static Task<Disposable> CreateAsync()
    {
        return Task.FromResult(new Disposable());
    }
}";
            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(new[] { disposableCode, testCode }, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task AwaitingReadAsync()
        {
            var testCode = @"
using System.IO;
using System.Threading.Tasks;
  
internal static class Foo
{
    internal static async Task Bar()
    {
        ↓var stream = await ReadAsync(string.Empty);
    }

    internal static async Task<Stream> ReadAsync(string file)
    {
        var stream = new MemoryStream();
        using (var fileStream = File.OpenRead(file))
        {
            await fileStream.CopyToAsync(stream)
                            .ConfigureAwait(false);
        }

        stream.Position = 0;
        return stream;
    }
}";
            var expected = this.CSharpDiagnostic()
                   .WithLocationIndicated(ref testCode)
                   .WithMessage("Use using.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}