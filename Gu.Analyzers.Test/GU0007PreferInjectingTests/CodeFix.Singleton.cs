namespace Gu.Analyzers.Test.GU0007PreferInjectingTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static partial class CodeFix
{
    public static class SingletonTests
    {
        private const string Singleton = @"
namespace N
{
    public sealed class Singleton
    {
        public static readonly Singleton Instance = new Singleton();

        private Singleton()
        {
        }
    }
}";

        private const string AbstractC = @"
namespace N
{
    public abstract class AbstractC
    {
        private readonly Singleton singleton;

        protected AbstractC(Singleton singleton)
        {
            this.singleton = singleton;
        }
    }
}";

        [Test]
        public static void AssigningThisFieldInCtor()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly Singleton value;

        public C()
        {
            this.value = Singleton.↓Instance;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly Singleton value;

        public C(Singleton value)
        {
            this.value = value;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Singleton, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void AssigningUnderscoreFieldInCtor()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly Singleton _value;

        public C()
        {
            _value = Singleton.↓Instance;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly Singleton _value;

        public C(Singleton value)
        {
            _value = value;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Singleton, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void WhenNotInjectingFieldInitializationObject()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly object bar;

        public C()
        {
            this.bar = Singleton.↓Instance;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly object bar;

        public C(Singleton bar)
        {
            this.bar = bar;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Singleton, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void FieldInitializationAndBaseCall()
        {
            var before = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Singleton singleton;

        public C()
            : base(Singleton.↓Instance)
        {
            this.singleton = Singleton.↓Instance;
        }
    }
}";

            var after = @"
namespace N
{
    public class C : AbstractC
    {
        private readonly Singleton singleton;

        public C(Singleton singleton)
            : base(singleton)
        {
            this.singleton = singleton;
        }
    }
}";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { AbstractC, Singleton, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void UsingInMethodWhenCtorExists()
        {
            var before = @"
namespace N
{
    public class C
    {
        public C()
        {
        }

        public string? M()
        {
            return Singleton.↓Instance.ToString();
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly Singleton singleton;

        public C(Singleton singleton)
        {
            this.singleton = singleton;
        }

        public string? M()
        {
            return this.singleton.ToString();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Singleton, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void UsingInMethodExpressionBodyWhenCtorExists()
        {
            var before = @"
namespace N
{
    public class C
    {
        public C()
        {
        }

        public string? M() => Singleton.↓Instance.ToString();
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly Singleton singleton;

        public C(Singleton singleton)
        {
            this.singleton = singleton;
        }

        public string? M() => this.singleton.ToString();
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Singleton, before }, after, fixTitle: "Inject safe.");
        }

        [Ignore("Not creating ctors yet.")]
        [Test]
        public static void UsingInMethodWhenNoCtor()
        {
            var before = @"
namespace N
{
    public class C
    {
        public string M()
        {
            return Singleton.↓Instance.ToString();
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly Singleton singleton;

        public C(Singleton singleton)
        {
            this.singleton = singleton;
        }

        public void M()
        {
            this.singleton.ToString();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { Singleton, before }, after, fixTitle: "Inject safe.");
        }
    }
}
