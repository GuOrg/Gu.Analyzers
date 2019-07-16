namespace Gu.Analyzers.Test.GU0007PreferInjectingTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        private const string SingletonCode = @"
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

        private static readonly string FooBaseCode = @"
namespace N
{
    public abstract class FooBase
    {
        private readonly Singleton singleton;

        protected FooBase(Singleton singleton)
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
    public class Foo
    {
        private readonly Singleton value;

        public Foo()
        {
            this.value = Singleton.↓Instance;
        }
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly Singleton value;

        public Foo(Singleton value)
        {
            this.value = value;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { SingletonCode, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void AssigningUnderscoreFieldInCtor()
        {
            var before = @"
namespace N
{
    public class Foo
    {
        private readonly Singleton _value;

        public Foo()
        {
            _value = Singleton.↓Instance;
        }
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly Singleton _value;

        public Foo(Singleton value)
        {
            _value = value;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { SingletonCode, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void WhenNotInjectingFieldInitializationObject()
        {
            var before = @"
        namespace N
        {
            public class Foo
            {
                private readonly object bar;

                public Foo()
                {
                    this.bar = Singleton.↓Instance;
                }
            }
        }";

            var after = @"
        namespace N
        {
            public class Foo
            {
                private readonly object bar;

                public Foo(Singleton bar)
                {
                    this.bar = bar;
                }
            }
        }";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { SingletonCode, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void FieldInitializationAndBaseCall()
        {
            var before = @"
        namespace N
        {
            public class Foo : FooBase
            {
                private readonly Singleton singleton;

                public Foo()
                    : base(Singleton.↓Instance)
                {
                    this.singleton = Singleton.↓Instance;
                }
            }
        }";

            var after = @"
        namespace N
        {
            public class Foo : FooBase
            {
                private readonly Singleton singleton;

                public Foo(Singleton singleton)
                    : base(singleton)
                {
                    this.singleton = singleton;
                }
            }
        }";
            RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, new[] { FooBaseCode, SingletonCode, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void UsingInMethodWhenCtorExists()
        {
            var before = @"
namespace N
{
    public class Foo
    {
        public Foo()
        {
        }

        public string M()
        {
            return Singleton.↓Instance.ToString();
        }
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly Singleton singleton;

        public Foo(Singleton singleton)
        {
            this.singleton = singleton;
        }

        public string M()
        {
            return this.singleton.ToString();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { SingletonCode, before }, after, fixTitle: "Inject safe.");
        }

        [Test]
        public static void UsingInMethodExpressionBodyWhenCtorExists()
        {
            var before = @"
namespace N
{
    public class Foo
    {
        public Foo()
        {
        }

        public string M() => Singleton.↓Instance.ToString();
    }
}";

            var after = @"
namespace N
{
    public class Foo
    {
        private readonly Singleton singleton;

        public Foo(Singleton singleton)
        {
            this.singleton = singleton;
        }

        public string M() => this.singleton.ToString();
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { SingletonCode, before }, after, fixTitle: "Inject safe.");
        }

        [Explicit("Not creating ctors yet.")]
        [Test]
        public static void UsingInMethodWhenNoCtor()
        {
            var before = @"
namespace N
{
    public class Foo
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
    public class Foo
    {
        private readonly Singleton singleton;

        public Foo(Singleton singleton)
        {
            this.singleton = singleton;
        }

        public void M()
        {
            this.singleton.ToString();
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { SingletonCode, before }, after, fixTitle: "Inject safe.");
        }
    }
}
