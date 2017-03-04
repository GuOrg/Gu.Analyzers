namespace Gu.Analyzers.Test.GU0032DisposeBeforeReassigningTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class CodeFix : CodeFixVerifier<GU0032DisposeBeforeReassigning, DisposeBeforeAssignCodeFixProvider>
    {
        internal class RefAndOut : NestedCodeFixVerifier<CodeFix>
        {
            [Test]
            public async Task AssigningFieldViaOutParameterInPublicMethod()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public void Update()
    {
        TryGetStream(↓out stream);
    }

    public bool TryGetStream(out Stream outValue)
    {
        outValue = File.OpenRead(string.Empty);
        return true;
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Dispose before re-assigning.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    private Stream stream;

    public void Update()
    {
        stream?.Dispose();
        TryGetStream(out stream);
    }

    public bool TryGetStream(out Stream outValue)
    {
        outValue = File.OpenRead(string.Empty);
        return true;
    }
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningVariableViaOutParameterBefore()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    public void Update()
    {
        Stream stream;
        if (TryGetStream(out stream))
        {
            ↓stream = File.OpenRead(string.Empty);
        }
    }

    public bool TryGetStream(out Stream result)
    {
        result = File.OpenRead(string.Empty);
        return true;
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Dispose before re-assigning.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    public void Update()
    {
        Stream stream;
        TryGetStream(out stream);
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
    }

    public bool TryGetStream(out Stream stream)
    {
        stream = File.OpenRead(string.Empty);
        return true;
    }
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
            }

            [Test]
            public async Task AssigningVariableViaOutParameterAfter()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    public void Update()
    {
        Stream stream = File.OpenRead(string.Empty);
        TryGetStream(↓out stream);
    }

    public bool TryGetStream(out Stream stream)
    {
        stream = File.OpenRead(string.Empty);
        return true;
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Dispose before re-assigning.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    public void Update()
    {
        Stream stream = File.OpenRead(string.Empty);
        stream?.Dispose();
        TryGetStream(out stream);
    }

    public bool TryGetStream(out Stream stream)
    {
        stream = File.OpenRead(string.Empty);
        return true;
    }
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
            }

            [Test]
            public async Task PublicMethodRefParameter()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo
{
    public bool TryGetStream(ref Stream stream)
    {
        ↓stream = File.OpenRead(string.Empty);
        return true;
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Dispose before re-assigning.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
using System;
using System.IO;

public class Foo
{
    public bool TryGetStream(ref Stream stream)
    {
        stream?.Dispose();
        stream = File.OpenRead(string.Empty);
        return true;
    }
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true).ConfigureAwait(false);
            }

            [Test]
            public async Task CallPrivateMethodRefParameter()
            {
                var testCode = @"
using System;
using System.IO;

public class Foo : IDisposable
{
    private Stream stream = File.OpenRead(string.Empty);

    private Foo()
    {
        this.Assign(↓ref this.stream);
    }

    public void Dispose()
    {
        stream?.Dispose();
    }

    private void Assign(ref Stream stream)
    {
        stream = File.OpenRead(string.Empty);
    }
}";

                var expected = this.CSharpDiagnostic()
                                   .WithLocationIndicated(ref testCode)
                                   .WithMessage("Dispose before re-assigning.");
                await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

                var fixedCode = @"
using System;
using System.IO;

public class Foo : IDisposable
{
    private Stream stream = File.OpenRead(string.Empty);

    private Foo()
    {
        this.stream?.Dispose();
        this.Assign(ref this.stream);
    }

    public void Dispose()
    {
        stream?.Dispose();
    }

    private void Assign(ref Stream stream)
    {
        stream = File.OpenRead(string.Empty);
    }
}";
                await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true).ConfigureAwait(false);
            }
        }
    }
}