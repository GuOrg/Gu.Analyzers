namespace Gu.Analyzers.Test.GU0073MemberShouldBeInternalTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0073MemberShouldBeInternal();

        [TestCase("internal readonly int F;")]
        [TestCase("internal event Action E;")]
        [TestCase("internal C() { }")]
        [TestCase("internal int P { get; }")]
        [TestCase("internal void M() { }")]
        [TestCase("internal enum E { }")]
        [TestCase("internal struct S { }")]
        [TestCase("internal class Nested { }")]
        public static void InternalClass(string member)
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal readonly int I; 
    }
}".AssertReplace("internal readonly int I;", member);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("public readonly int F;")]
        [TestCase("public event Action E;")]
        [TestCase("public C() { }")]
        [TestCase("public int P { get; }")]
        [TestCase("public void M() { }")]
        public static void PublicClass(string member)
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public readonly int I;
    }
}".AssertReplace("public readonly int I;", member);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InterfaceEvent()
        {
            var code = @"
namespace N
{
    using System;

    internal class C : IC
    {
        public event Action E;
    }

    public interface IC
    {
        event Action E;
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExplicitInterfaceEvent()
        {
            var code = @"
namespace N
{
    using System;

    internal class C : IC
    {
        private event Action E;

        event Action IC.E
        {
            add => this.E += value;
            remove => this.E -= value;
        }
    }

    public interface IC
    {
        event Action E;
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void OverridingEvent()
        {
            var code = @"
namespace N
{
    using System;

    internal class C : Abstract
    {
        public override event Action E;
    }

    public abstract class Abstract
    {
        public abstract event Action E;
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InterfaceProperty()
        {
            var code = @"
namespace N
{
    internal class C : IC
    {
        public int P { get; }
    }

    public interface IC
    {
        int P { get; }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExplicitInterfaceProperty()
        {
            var code = @"
namespace N
{
    internal class C : IC
    {
        int IC.P { get; }
    }

    public interface IC
    {
        int P { get; }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void OverridingProperty()
        {
            var code = @"
namespace N
{
    internal class C : Abstract
    {
        public override int P { get; }
    }

    public abstract class Abstract
    {
        public abstract int P { get; }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InterfaceMethod()
        {
            var code = @"
namespace N
{
    using System;

    internal class C : IDisposable
    {
        public void Dispose() { }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void GenericInterfaceMethod()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class C : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
