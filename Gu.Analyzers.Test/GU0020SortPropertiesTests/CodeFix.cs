namespace Gu.Analyzers.Test.GU0020SortPropertiesTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class CodeFix
{
    private static readonly GU0020SortProperties Analyzer = new();
    private static readonly SortPropertiesFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0020SortProperties);

    [Test]
    public static void MutableBeforeGetOnlySimple()
    {
        var before = @"
namespace N
{
    public class C
    {
        ↓public int B { get; set; }

        public int A { get; }
    }
}";

        var after = @"
namespace N
{
    public class C
    {
        public int A { get; }

        public int B { get; set; }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void MutableBeforeGetOnlySimpleWithDocs()
    {
        var before = @"
namespace N
{
    public class C
    {
        /// <summary>
        /// B
        /// </summary>
        ↓public int B { get; set; }

        /// <summary>
        /// A
        /// </summary>
        public int A { get; }
    }
}";

        var after = @"
namespace N
{
    public class C
    {
        /// <summary>
        /// A
        /// </summary>
        public int A { get; }

        /// <summary>
        /// B
        /// </summary>
        public int B { get; set; }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void ExplicitImplementation()
    {
        var interfaceCode = @"
namespace N
{
    interface IValue
    {
        object Value { get; }
    }
}";

        var before = @"
namespace N
{
    public class C : IValue
    {
        ↓private int Value { get; } = 5;

        object IValue.Value { get; } = 5;
    }
}";

        var after = @"
namespace N
{
    public class C : IValue
    {
        object IValue.Value { get; } = 5;

        private int Value { get; } = 5;
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, new[] { interfaceCode, before }, after);
    }

    [Test]
    public static void MutableBeforeGetOnlyFirst()
    {
        var before = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        ↓public int A { get; set; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";

        var after = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public int A { get; set; }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void MutableBeforeGetOnlyFirstWithNamespaces()
    {
        var before = @"
namespace N
{
    using System;

    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        ↓public int A { get; set; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";

        var after = @"
namespace N
{
    using System;

    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public int A { get; set; }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void MutableBeforeGetOnlyLast()
    {
        var before = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        ↓public int C { get; set; }

        public int D { get; }
    }
}";

        var after = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int D { get; }

        public int C { get; set; }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void PrivateSetAfterPublicSet()
    {
        var before = @"
namespace N
{
    public class Foo
    {
        private int c;
        private int d;

        public int A { get; }

        public int B
        {
            get
            {
                return this.A;
            }
        }

        ↓public int C
        {
            get
            {
                return this.c;
            }
            set
            {
                this.c = value;
            }
        }

        public int D
        {
            get
            {
                return this.d;
            }
            private set
            {
                this.d = value;
            }
        }
    }
}";

        var after = @"
namespace N
{
    public class Foo
    {
        private int c;
        private int d;

        public int A { get; }

        public int B
        {
            get
            {
                return this.A;
            }
        }

        public int D
        {
            get
            {
                return this.d;
            }
            private set
            {
                this.d = value;
            }
        }

        public int C
        {
            get
            {
                return this.c;
            }
            set
            {
                this.c = value;
            }
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void MutableBeforeGetOnlyFirstWithInitializers()
    {
        var before = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        ↓public int A { get; set; } = 1;

        public int B { get; } = 2;

        public int C { get; } = 3;

        public int D { get; } = 4;
    }
}";

        var after = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int B { get; } = 2;

        public int C { get; } = 3;

        public int D { get; } = 4;

        public int A { get; set; } = 1;
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void MutableBeforeGetOnlyWithComments()
    {
        var before = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        /// <summary>
        /// C
        /// </summary>
        ↓public int C { get; set; }

        /// <summary>
        /// D
        /// </summary>
        public int D { get; }
    }
}";

        var after = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        /// <summary>
        /// D
        /// </summary>
        public int D { get; }

        /// <summary>
        /// C
        /// </summary>
        public int C { get; set; }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void ExpressionBodyBeforeGetOnly()
    {
        var before = @"
namespace N
{
    public class C
    {
        public C(int b)
        {
            this.B = b;
        }

        ↓public int A => B;

        public int B { get; }
    }
}";

        var after = @"
namespace N
{
    public class C
    {
        public C(int b)
        {
            this.B = b;
        }

        public int B { get; }

        public int A => B;
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void CalculatedBeforeGetOnly()
    {
        var before = @"
namespace N
{
    public class C
    {
        public C(int b)
        {
            this.B = b;
        }

        ↓public int A
        {
            get
            {
                return B;
            }
        }

        public int B { get; }
    }
}";

        var after = @"
namespace N
{
    public class C
    {
        public C(int b)
        {
            this.B = b;
        }

        public int B { get; }

        public int A
        {
            get
            {
                return B;
            }
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void IndexerBeforeMutable()
    {
        var before = @"
namespace N
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class C : IReadOnlyList<int>
    {
        public int Count { get; }

        ↓public int this[int index]
        {
            get
            {
                if (index != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, ""message"");
                }

                return A;
            }
        }

        public int A { get; set; }

        public IEnumerator<int> GetEnumerator()
        {
            yield return A;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}";

        var after = @"
namespace N
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class C : IReadOnlyList<int>
    {
        public int Count { get; }

        public int A { get; set; }

        public int this[int index]
        {
            get
            {
                if (index != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, ""message"");
                }

                return A;
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            yield return A;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void PublicSetBeforePrivateSetFirst()
    {
        var before = @"
namespace N
{
    public class C
    {
        public C(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        ↓public int A { get; set; }

        public int B { get; private set; }
    }
}";

        var after = @"
namespace N
{
    public class C
    {
        public C(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int B { get; private set; }

        public int A { get; set; }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void NestedClass()
    {
        var before = @"
namespace N
{
    public sealed class C
    {
        public C()
        {
        }

        public int Bar
        {
            get { return this.Bar; }
            set { this.Bar = value; }
        }

        public class Nested
        {
            ↓public int Value1 { get; set; }
            
            public int Value2 { get; private set; }
        }
    }
}";

        var after = @"
namespace N
{
    public sealed class C
    {
        public C()
        {
        }

        public int Bar
        {
            get { return this.Bar; }
            set { this.Bar = value; }
        }

        public class Nested
        {
            public int Value2 { get; private set; }

            public int Value1 { get; set; }
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }
}
