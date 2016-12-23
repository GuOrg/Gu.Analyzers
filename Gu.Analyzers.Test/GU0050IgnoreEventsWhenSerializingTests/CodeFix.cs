﻿namespace Gu.Analyzers.Test.GU0050IgnoreEventsWhenSerializingTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0050IgnoreEventsWhenSerializing, UseGetOnlyCodeFixProvider>
    {
        [Test]
        public async Task NotIgnoredEvent()
        {
            var testCode = @"
using System;

[Serializable]
public class Foo
{
    public Foo(int a, int b, int c, int d)
    {
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
    }

    public event EventHandler SomeEvent;

    public int A { get; }

    public int B { get; protected set;}

    public int C { get; internal set; }

    public int D { get; set; }

    public int E => A;
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Ignore events when serializing.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
using System;

[Serializable]
public class Foo
{
    public Foo(int a, int b, int c, int d)
    {
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
    }

    [field:NonSerialized]
    public event EventHandler SomeEvent;

    public int A { get; }

    public int B { get; protected set;}

    public int C { get; internal set; }

    public int D { get; set; }

    public int E => A;
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NotIgnoredEventHandler()
        {
            var testCode = @"
[Serializable]
public class Foo
{
    private EventHandler someEvent;

    public event EventHandler SomeEvent
    {
        add { this.someEvent += value; }
        remove { this.someEvent -= value; }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Ignore events when serializing.");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"
[Serializable]
public class Foo
{
    [NonSerialized]
    private EventHandler someEvent;

    public event EventHandler SomeEvent
    {
        add { this.someEvent += value; }
        remove { this.someEvent -= value; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
