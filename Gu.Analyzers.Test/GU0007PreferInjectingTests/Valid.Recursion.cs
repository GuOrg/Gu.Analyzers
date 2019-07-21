namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class Valid
    {
        internal static class Recursion
        {
            [Test]
            public static void IgnoresRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable meh1;

        public C()
        {
            this.meh1 = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenDisposingFieldAssignedWithRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private IDisposable disposable;

        public C()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenNotDisposingFieldAssignedWithRecursiveProperty()
            {
                var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private IDisposable disposable;

        public C()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void IgnoresWhenDisposingRecursiveMethod()
            {
                var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable RecursiveMethod() => RecursiveMethod();

        public void Dispose()
        {
            this.RecursiveMethod().Dispose();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }
        }
    }
}
