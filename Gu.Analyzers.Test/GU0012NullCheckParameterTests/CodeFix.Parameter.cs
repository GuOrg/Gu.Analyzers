namespace Gu.Analyzers.Test.GU0012NullCheckParameterTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static partial class CodeFix
    {
        internal static class Parameter
        {
            private static readonly DiagnosticAnalyzer Analyzer = new ParameterAnalyzer();
            private static readonly CodeFixProvider Fix = new NullCheckParameterFix();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0012NullCheckParameter);

            [TestCase("public")]
            [TestCase("internal")]
            [TestCase("protected")]
            public static void SimpleAssignFullyQualified(string access)
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string ↓text)
        {
            this.text = text;
        }
    }
}".AssertReplace("public C", $"{access} C");

                var after = @"
namespace N
{
    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text ?? throw new System.ArgumentNullException(nameof(text));
        }
    }
}".AssertReplace("public C", $"{access} C");

                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void SimpleAssignWhenUsingExists()
            {
                var before = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string ↓text)
        {
            this.text = text;
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string text;

        public C(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void WhenKeyword()
            {
                var before = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string @default;

        public C(string ↓@default)
        {
            this.@default = @default;
        }
    }
}";

                var after = @"
namespace N
{
    using System;

    public class C
    {
        private readonly string @default;

        public C(string @default)
        {
            this.@default = @default ?? throw new ArgumentNullException(nameof(@default));
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIfNullThrow()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly int bar;

        public C(string ↓text)
        {
            this.bar = M(text);
        }

        private int M(string text) => text.Length;
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly int bar;

        public C(string text)
        {
            if (text is null)
            {
                throw new System.ArgumentNullException(nameof(text));
            }

            this.bar = M(text);
        }

        private int M(string text) => text.Length;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIfNullThrowWhenComment()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly int bar;

        public C(string ↓text)
        {
            // comment
            this.bar = M(text);
        }

        private int M(string text) => text.Length;
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly int bar;

        public C(string text)
        {
            if (text is null)
            {
                throw new System.ArgumentNullException(nameof(text));
            }

            // comment
            this.bar = M(text);
        }

        private int M(string text) => text.Length;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void AddIfNullThrowWhenKeyword()
            {
                var before = @"
namespace N
{
    public class C
    {
        private readonly int f;

        public C(string ↓@default)
        {
            this.f = M(@default);
        }

        private static int M(string s) => s.Length;
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        private readonly int f;

        public C(string @default)
        {
            if (@default is null)
            {
                throw new System.ArgumentNullException(nameof(@default));
            }

            this.f = M(@default);
        }

        private static int M(string s) => s.Length;
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void WhenNotUsed()
            {
                var before = @"
namespace N
{
    public class C
    {
        public C(string ↓text)
        {
        }
    }
}";

                var after = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            if (text is null)
            {
                throw new System.ArgumentNullException(nameof(text));
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [TestCase("s1 is null")]
            [TestCase("s1 == null")]
            [TestCase("ReferenceEquals(s1, null)")]
            public static void AfterOtherParameter(string expression)
            {
                var before = @"
namespace N
{
    public sealed class C
    {
        public C(string s1, string ↓s2)
        {
            if (s1 is null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }
        }
    }
}".AssertReplace("s1 is null", expression);

                var after = @"
namespace N
{
    public sealed class C
    {
        public C(string s1, string s2)
        {
            if (s1 is null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }

            if (s2 is null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}".AssertReplace("s1 is null", expression);
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void BeforeOtherParameter()
            {
                var before = @"
namespace N
{
    public sealed class C
    {
        public C(string ↓s1, string s2)
        {
            if (s2 is null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";

                var after = @"
namespace N
{
    public sealed class C
    {
        public C(string s1, string s2)
        {
            if (s1 is null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }

            if (s2 is null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void FixAll()
            {
                var before = @"
namespace N
{
    public sealed class C
    {
        public C(string ↓s1, string ↓s2)
        {
        }
    }
}";

                var after = @"
namespace N
{
    public sealed class C
    {
        public C(string s1, string s2)
        {
            if (s1 is null)
            {
                throw new System.ArgumentNullException(nameof(s1));
            }

            if (s2 is null)
            {
                throw new System.ArgumentNullException(nameof(s2));
            }
        }
    }
}";
                RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }

            [Test]
            public static void OutParameter()
            {
                var before = @"
namespace N
{
    public sealed class C
    {
        private readonly string s1;

        public C(string ↓s1, out string s2)
        {
            this.s1 = s1;
            s2 = s1;
        }
    }
}";

                var after = @"
namespace N
{
    public sealed class C
    {
        private readonly string s1;

        public C(string s1, out string s2)
        {
            this.s1 = s1 ?? throw new System.ArgumentNullException(nameof(s1));
            s2 = s1;
        }
    }
}";
                RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
            }
        }
    }
}
