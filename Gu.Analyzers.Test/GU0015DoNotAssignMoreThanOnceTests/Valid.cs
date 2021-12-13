namespace Gu.Analyzers.Test.GU0015DoNotAssignMoreThanOnceTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly SimpleAssignmentAnalyzer Analyzer = new();

        [Test]
        public static void SimpleAssign()
        {
            var code = @"
namespace N
{
    public class C
    {
        private readonly int value;

        public C(int value)
        {
            this.value = value;
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void AssignInIfElse()
        {
            var code = @"
namespace N
{
    public class C
    {
        private readonly int value;

        public C(int value)
        {
            if (value < 0)
            {
                this.value = 0;
            }
            else
            {
                this.value = value;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreMutableInLambda()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C(int value)
        {
            this.Value = value;
            System.Console.CancelKeyPress += (_, __) => Console.WriteLine(this.Value);
        }

        public int Value { get; set; }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreOutParameter()
        {
            var code = @"
namespace N
{
    public class C
    {
        public static bool TryGet(int i, out string? text)
        {
            text = null;
            if (i > 10)
            {
                text = i.ToString();
            }

            return text != null;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreOutParameterIfReturn()
        {
            var code = @"
namespace N
{
    public class C
    {
        public static bool TryGet(int i, out string? text)
        {
            if (i > 10)
            {
                text = i.ToString();
                return true;
            }

            if (i < 10)
            {
                text = null;
                return false;
            }

            text = ""0"";
            return true;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoreObjectInitializer()
        {
            var code = @"
namespace N
{
    public class C1
    {
        public C1()
        {
            var bar1 = new C2 { Value = 1 };
            var bar2 = new C2 { Value = 1 };
        }
    }

    public class C2
    {
        public int Value { get; set; }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DifferentInstances()
        {
            var code = @"
namespace N
{
    public class C1
    {
        public C1(C2 bar1, C2 bar2)
        {
            bar1.Value = 1;
            bar2.Value = 3;
        }
    }

    public class C2
    {
        public int Value { get; set; }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Enum()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void Run()
        {
            this.Status = Status.Running;
            _ = Console.ReadKey();
            this.Status = Status.Finished;
        }

        public Status Status { get; private set; }
    }

    public enum Status
    {
        Running,
        Finished,
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Boolean()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void Run()
        {
            this.Running = true;
            _ = Console.ReadKey();
            this.Running = false;
        }

        public bool Running { get; private set; }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenUsingTheValueStringReplace()
        {
            var code = @"
namespace N
{
    public class C
    {
        public static string M(string text)
        {
            text = text.Replace("" "", ""_"");
            text = text.Replace("":"", ""_"");
            return text;
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenUsingTheValue1()
        {
            var code = @"
namespace N
{
    public class C
    {
        public int Value1 { get; }

        public int Value2 { get; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = this.Value1 * 397;
                hash = hash ^ this.Value2;
                return hash;
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenUsingTheValue2()
        {
            var code = @"
#nullable disable
namespace N
{
    public class C
    {
        public C(int value1, int value2)
        {
            this.Value1 = value1;
            this.Value2 = value2;
        }

        public int Value1 { get; }

        public int Value2 { get; }

        public static bool operator ==(C left, C right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(C left, C right)
        {
            return !Equals(left, right);
        }

        protected bool Equals(C other)
        {
            return this.Value1 == other.Value1 && this.Value2 == other.Value2;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((C)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = this.Value1 * 397;
                hash = hash ^ this.Value2;
                return hash;
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
