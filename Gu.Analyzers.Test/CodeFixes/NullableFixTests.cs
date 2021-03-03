namespace Gu.Analyzers.Test.CodeFixes
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal static class NullableFixTests
    {
        private static readonly CodeFixProvider Fix = new NullableFix();
        private static readonly ExpectedDiagnostic CS8600 = ExpectedDiagnostic.Create("CS8600");
        private static readonly ExpectedDiagnostic CS8601 = ExpectedDiagnostic.Create("CS8601");
        private static readonly ExpectedDiagnostic CS8625 = ExpectedDiagnostic.Create("CS8625");
        private static readonly ExpectedDiagnostic CS8618 = ExpectedDiagnostic.Create("CS8618", "Non-nullable event 'E' must contain a non-null value when exiting constructor. Consider declaring the event as nullable.");
        private static readonly ExpectedDiagnostic CS8765 = ExpectedDiagnostic.Create("CS8765");
        private static readonly CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);

        [Test]
        public static void AddNotNullWhenAndUsing()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static bool Try(string s, out string result)
        {
            if (s.Length > 2)
            {
                result = s;
                return true;
            }

            result = ↓null;
            return false;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(string s, [NotNullWhen(true)] out string? result)
        {
            if (s.Length > 2)
            {
                result = s;
                return true;
            }

            result = null;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8625, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void NoFixWhenLocalFunction()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            _ = Try(string.Empty, out _);

            bool Try(string s, out string result)
            {
                if (s.Length > 2)
                {
                    result = s;
                    return true;
                }

                result = ↓null;
                return false;
            }
        }
    }
}";

            RoslynAssert.NoFix(new PlaceholderAnalyzer(CS8625.Id), Fix, CS8625, before, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void AddNotNullWhenTrueWhenAssigningOutParameterWithNullLiteral()
        {
            var before = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(string s, out string result)
        {
            _ = typeof(NotNullWhenAttribute);
            if (s.Length > 2)
            {
                result = s;
                return true;
            }

            result = ↓null;
            return false;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(string s, [NotNullWhen(true)] out string? result)
        {
            _ = typeof(NotNullWhenAttribute);
            if (s.Length > 2)
            {
                result = s;
                return true;
            }

            result = null;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8625, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void AddNotNullWhenTrueWhenAssigningOutParameterWithAs()
        {
            var before = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(object o, out string result)
        {
            _ = typeof(NotNullWhenAttribute);
            result = ↓o as string;
            return result != null;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try(object o, [NotNullWhen(true)] out string? result)
        {
            _ = typeof(NotNullWhenAttribute);
            result = o as string;
            return result != null;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8601, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void AddNotNullWhenTrueWhenAssigningOutParameterWithOut()
        {
            var before = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try1(object o, out string result)
        {
            return Try2(o, out ↓result);
        }

        public static bool Try2(object o, [NotNullWhen(true)] out string? result)
        {
            result = o as string;
            return result != null;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try1(object o, [NotNullWhen(true)] out string? result)
        {
            return Try2(o, out result);
        }

        public static bool Try2(object o, [NotNullWhen(true)] out string? result)
        {
            result = o as string;
            return result != null;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8601, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void FlowOutType()
        {
            var before = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try1(object o)
        {
            return Try2(o, out ↓string result);
        }

        public static bool Try2(object o, [NotNullWhen(true)] out string? result)
        {
            result = o as string;
            return result != null;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try1(object o)
        {
            return Try2(o, out string? result);
        }

        public static bool Try2(object o, [NotNullWhen(true)] out string? result)
        {
            result = o as string;
            return result != null;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8600, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void AddMaybeNullWhenFalse()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static bool Try<T>(T s, bool b, out T result)
        {
            if (b)
            {
                result = s;
                return true;
            }

            result = ↓default;
            return false;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try<T>(T s, bool b, [MaybeNullWhen(false)] out T result)
        {
            if (b)
            {
                result = s;
                return true;
            }

            result = default!;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8601, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void AddMaybeNullWhenFalseDefaultOfT()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static bool Try<T>(T s, bool b, out T result)
        {
            if (b)
            {
                result = s;
                return true;
            }

            result = ↓default(T);
            return false;
        }
    }
}";

            var after = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;

    public static class C
    {
        public static bool Try<T>(T s, bool b, [MaybeNullWhen(false)] out T result)
        {
            if (b)
            {
                result = s;
                return true;
            }

            result = default!;
            return false;
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8601, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void MakeOptionalParameterNullable()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M(string s = ↓null)
        {
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M(string? s = null)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8625, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void MakeEventNullable()
        {
            var before = @"
namespace N
{
    using System;

    class C
    {
        event Action ↓E;

        private void OnE()
        {
            this.E?.Invoke();
        }
    }
}";

            var after = @"
namespace N
{
    using System;

    class C
    {
        event Action? E;

        private void OnE()
        {
            this.E?.Invoke();
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8618, before, after, compilationOptions: CompilationOptions);
        }

        [Test]
        public static void Equals()
        {
            var before = @"
namespace N
{
    using System;

    public struct C : IEquatable<C>
    {
        public static bool operator ==(C left, C right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(C left, C right)
        {
            return !left.Equals(right);
        }

        public bool Equals(C other)
        {
            throw new NotImplementedException();
        }

        public override bool ↓Equals(object obj)
        {
            return obj is C other && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}";

            var after = @"
namespace N
{
    using System;

    public struct C : IEquatable<C>
    {
        public static bool operator ==(C left, C right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(C left, C right)
        {
            return !left.Equals(right);
        }

        public bool Equals(C other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object? obj)
        {
            return obj is C other && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS8765, before, after, compilationOptions: CompilationOptions);
        }
    }
}
