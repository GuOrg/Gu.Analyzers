namespace Gu.Analyzers.Test.GU0006UseNameofTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly StringLiteralExpressionAnalyzer Analyzer = new();

        [Test]
        public static void WhenThrowingArgumentException()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ArgumentOutOfRangeException()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M(StringComparison value)
        {
            switch (value)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresDebuggerDisplay()
        {
            var code = @"
namespace N
{
    [System.Diagnostics.DebuggerDisplay(""{Name}"")]
    public class C
    {
        public string Name => string.Empty;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresTypeName()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M()
        {
            this.M(""Exception"");
        }

        public void M(string value)
        {
            throw new ArgumentException(nameof(value), value);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresSameLocal()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C()
        {
#pragma warning disable CS0219
            var text = ""text"";
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenUsedInDeclaration()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C()
        {
            var text = Id(""text"");
        }

        private static string Id(string value) => value;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenLocalsNotVisible()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C()
        {
            {
                var text = string.Empty;
            }

            {
                var text = Id(""text"");
            }

            {
                var text = string.Empty;
            }
        }

        private static string Id(string value) => value;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void IgnoresNamespaceName()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public void M()
        {
            this.M(""Test"");
        }

        public void M(string value)
        {
            throw new ArgumentException(nameof(value), value);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
