namespace Gu.Analyzers.Test.GU0022UseGetOnlyTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly GU0022UseGetOnly Analyzer = new GU0022UseGetOnly();

        private static readonly TestCase[] TestCases =
        {
            new TestCase("int", "A++;"),
            new TestCase("int", "A--;"),
            new TestCase("int", "A+=a;"),
            new TestCase("int", "A-=a;"),
            new TestCase("int", "A*=a;"),
            new TestCase("int", "A/=a;"),
            new TestCase("int", "A%=a;"),
            new TestCase("int", "A = a;"),
            new TestCase("bool", "A|=a;"),
        };

        [TestCaseSource(nameof(TestCases))]
        public static void UpdatedInMethodThis(TestCase data)
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public int A { get; private set; }

        public void Update(int a)
        {
            this.A = a;
        }
    }
}".AssertReplace("A = a;", data.Update)
  .AssertReplace("int", data.Type);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCaseSource(nameof(TestCases))]
        public static void UpdatedInMethodUnderscoreNames(TestCase data)
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public int A { get; private set; }

        public void Update(int a)
        {
            A = a;
        }
    }
}".AssertReplace("A = a;", data.Update)
  .AssertReplace("int", data.Type);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCaseSource(nameof(TestCases))]
        public static void UpdatingOtherInstanceInCtor(TestCase data)
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public Foo(Foo previous, int a)
        {
            previous.A = a;
        }

        public int A { get; private set; }
    }
}".AssertReplace("A = a;", data.Update)
  .AssertReplace("int", data.Type);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void UpdatedInLambdaInCtor()
        {
            var code = @"
namespace N
{
    using System;

    public class Foo
    {
        public Foo()
        {
            this.E += (_, __) => this.A = 5;
        }

        public event EventHandler E;

        public int A { get; private set; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DifferentProperties()
        {
            var interfaceCode = @"
namespace N
{
    public interface IFoo
    {
        int D { get; set; }
    }
}";
            var testCode = @"
namespace N
{
    public class Foo : IFoo
    {
        // private int f;
        
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; protected set;}

        public int C { get; internal set; }

        public int D { get; set; }

        int IFoo.D
        {
            get { return this.D; }
            set { this.D = value; }
        }

        public int E => A;

        // public int F
        // {
        //     get => this.f;
        //     set => this.f = value;
        // }
    }
}";
            RoslynAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public static void OtherInstanceObjectInitializer()
        {
            var code = @"
namespace N
{
    public sealed class Foo
    {
        public int Value { get; private set; }

        public static Foo Create(int value)
        {
            return new Foo { Value = value };
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SideEffectStaticMethod()
        {
            var code = @"
namespace N
{
    public sealed class Foo
    {
        public int Value { get; private set; }

        public static void Update(Foo foo)
        {
            foo.Value = 2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void SideEffectStaticMethodPrivateProperty()
        {
            var code = @"
namespace N
{
    public sealed class Foo
    {
        private int Value { get; set; }

        public static void Update(Foo foo)
        {
            foo.Value = 2;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AssignedInSetOnlyWithTernary()
        {
            var code = @"
namespace N
{
    public class Foo<T>
        where T : struct 
    {
        public T Value { get; private set; }

        public T? Meh
        {
            set { Value = value.HasValue ? value.Value : default(T); }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExplicitImplementation()
        {
            var code = @"
namespace N
{
    interface IFoo
    {
        object Value { get; set; }
    }

    class Foo<T> : IFoo
    {
        public T Value { get; private set; }

        object IFoo.Value
        {
            get { return this.Value; }
            set { this.Value = (T) value; }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        internal class TestCase
        {
            public TestCase(string type, string update)
            {
                this.Type = type;
                this.Update = update;
            }

            public string Type { get; }

            public string Update { get; }

            public override string ToString() => this.Update;
        }
    }
}
