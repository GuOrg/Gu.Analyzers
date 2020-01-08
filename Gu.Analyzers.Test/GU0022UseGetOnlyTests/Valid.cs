namespace Gu.Analyzers.Test.GU0022UseGetOnlyTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0022UseGetOnly Analyzer = new GU0022UseGetOnly();

        private static readonly TestCaseData[] TestCases =
        {
            new TestCaseData("int", "A++;"),
            new TestCaseData("int", "A--;"),
            new TestCaseData("int", "A+=a;"),
            new TestCaseData("int", "A-=a;"),
            new TestCaseData("int", "A*=a;"),
            new TestCaseData("int", "A/=a;"),
            new TestCaseData("int", "A%=a;"),
            new TestCaseData("int", "A = a;"),
            new TestCaseData("bool", "A|=a;"),
        };

        [TestCaseSource(nameof(TestCases))]
        public static void UpdatedInMethodThis(string type, string statement)
        {
            var code = @"
namespace N
{
    public class C
    {
        public int A { get; private set; }

        public void Update(int a)
        {
            this.A = a;
        }
    }
}".AssertReplace("A = a;", statement)
  .AssertReplace("int", type);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCaseSource(nameof(TestCases))]
        public static void UpdatedInMethodUnderscoreNames(string type, string statement)
        {
            var code = @"
namespace N
{
    public class C
    {
        public int A { get; private set; }

        public void Update(int a)
        {
            A = a;
        }
    }
}".AssertReplace("A = a;", statement)
  .AssertReplace("int", type);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCaseSource(nameof(TestCases))]
        public static void UpdatingOtherInstanceInCtor(string type, string statement)
        {
            var code = @"
namespace N
{
    public class C
    {
        public C(C previous, int a)
        {
            previous.A = a;
        }

        public int A { get; private set; }
    }
}".AssertReplace("A = a;", statement)
  .AssertReplace("int", type);

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void UpdatedInLambdaInCtor()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
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
            var iFoo = @"
namespace N
{
    public interface IFoo
    {
        int D { get; set; }
    }
}";
            var foo = @"
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
            RoslynAssert.Valid(Analyzer, iFoo, foo);
        }

        [Test]
        public static void OtherInstanceObjectInitializer()
        {
            var code = @"
namespace N
{
    public sealed class C
    {
        public int Value { get; private set; }

        public static C Create(int value)
        {
            return new C { Value = value };
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
    public sealed class C
    {
        public int Value { get; private set; }

        public static void Update(C foo)
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
    public sealed class C
    {
        private int Value { get; set; }

        public static void Update(C foo)
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
    public class C<T>
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
        public static void ExplicitImplementationStatementBodies()
        {
            var code = @"
namespace N
{
    interface IC
    {
        object Value { get; set; }
    }

    class C<T> : IC
    {
        public T Value { get; private set; }

        object IC.Value
        {
            get { return this.Value; }
            set { this.Value = (T)value; }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExplicitImplementationExpressionBodies()
        {
            var code = @"
namespace N
{
    interface IC
    {
        object Value { get; set; }
    }

    class C<T> : IC
    {
        public T Value { get; private set; }

        object IC.Value
        {
            get => this.Value;
            set => this.Value = (T)value;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
