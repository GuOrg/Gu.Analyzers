namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0020SortProperties Analyzer = new GU0020SortProperties();

        [Test]
        public static void WithCustomEvent()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        private EventHandler someEvent;

        public event EventHandler SomeEvent
        {
            add { this.someEvent += value; }
            remove { this.someEvent -= value; }
        }

        public int Value { get; set; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void GetOnlies()
        {
            var code = @"
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

        public int C { get; }

        public int D { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExplicitImplementationGetOnly()
        {
            var interfaceCode = @"
namespace N
{
    interface IValue
    {
        object Value { get; }
    }
}";

            var testCode = @"
namespace N
{
    public class C : IValue
    {
        public int Value { get; } = 5;

        object IValue.Value { get; } = 5;
    }
}";
            RoslynAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public static void ExplicitImplementationGetOnlyIndexer()
        {
            var interfaceCode = @"
namespace N
{
    interface IValue
    {
        object this[int index] { get; }
    }
}";

            var testCode = @"
namespace N
{
    public class C : IValue
    {
        public int this[int index] => index;

        object IValue.this[int index] => index;
    }
}";
            RoslynAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public static void ExplicitGetSetIndexerAndGetOnlyIndexer()
        {
            var interfaceCode = @"
namespace N
{
    interface IValue
    {
        int this[int index] { get; set; }
    }
}";

            var testCode = @"
namespace N
{
    public class C : IValue
    {
        public int this[int index] => index;

        int IValue.this[int index]
        {
            get { return this[index]; }
            set { return; }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public static void ExplicitGetSetIndexerAndGetSetIndexer()
        {
            var interfaceCode = @"
namespace N
{
    interface IValue
    {
        int this[int index] { get; set; }
    }
}";

            var testCode = @"
namespace N
{
    public class C : IValue
    {
        private int meh;

        public int this[int index]
        {
            get { return this.meh; }
            set { this.meh = index; }
        }

        int IValue.this[int index]
        {
            get { return this.meh; }
            set { this.meh = index; }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public static void ExplicitImplementationCalculatedBeforeGetSet()
        {
            var interfaceCode = @"
namespace N
{
    interface IValue
    {
        object Value { get; }
    }
}";

            var testCode = @"
namespace N
{
    public class C : IValue
    {
        object IValue.Value => this.Value;

        public int Value { get; set; }
    }
}";
            RoslynAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public static void Mutables()
        {
            var code = @"
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

        public int A { get; set; }

        public int B { get; set; }

        public int C { get; set; }

        public int D { get; set; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void NotifyingMutables()
        {
            var code = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class Foo : INotifyPropertyChanged
    {
        private int _a;
        private int _b;
        private int _c;
        private int _d;

        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int A
        {
            get
            {
                return _a;
            }
            set
            {
                if (value == _a) return;
                _a = value;
                OnPropertyChanged();
            }
        }

        public int B
        {
            get
            {
                return _b;
            }
            set
            {
                if (value == _b) return;
                _b = value;
                OnPropertyChanged();
            }
        }

        public int C
        {
            get
            {
                return _c;
            }
            set
            {
                if (value == _c) return;
                _c = value;
                OnPropertyChanged();
            }
        }

        public int D
        {
            get
            {
                return _d;
            }
            set
            {
                if (value == _d) return;
                _d = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void MutablesBySetter()
        {
            var code = @"
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

        public int A { get; private set; }

        public int B { get; private set; }

        public int C { get; set; }

        public int D { get; set; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExpressionBodies()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        private readonly int a;

        public Foo(int a)
        {
            this.a = a;
        }

        public int A => this.a;

        public int B => 2;

        public int C => this.a;

        public int D => this.a;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Calculated()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        public C(StringComparison stringComparison)
        {
            this.StringComparison = stringComparison;
        }

        public StringComparison StringComparison { get; }

        public bool IsCurrentCulture => this.StringComparison == StringComparison.CurrentCulture;

        public bool IsOrdinalIgnoreCase => this.StringComparison == StringComparison.OrdinalIgnoreCase;

        public bool IsOrdinal
        {
            get
            {
                return this.StringComparison == StringComparison.Ordinal;
            }
        }

        public bool IsInvariantCulture
        {
            get
            {
                return this.StringComparison == StringComparison.InvariantCulture;
            }
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void InternalBeforePublicIndexer()
        {
            var code = @"
namespace N
{
    public class C
    {
        internal int Value { get; set; }

        public int this[int index] => index;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StaticBeforeInstance()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C(int b)
        {
            this.B = b;
        }

        public static int A { get; } = 1;

        public int B { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void StaticGetOnlyBeforeCalculated()
        {
            var code = @"
namespace N
{
    public class C
    {
        public static int A { get; } = 1;

        public int B => A;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void PublicBeforeProtectedStatic()
        {
            var code = @"
namespace N
{
    public class C
    {
        public C(int a)
        {
            this.A = a;
        }

        public int A { get; }

        protected static int B { get; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void PublicInitializedWithProtectedStatic()
        {
            var code = @"
namespace N
{
    public class C
    {
        protected static int B { get; }

        public static int A { get; } = B;
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void Realistic()
        {
            var code = @"
namespace N
{
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }

        public int C => A;

        public int D
        {
            get
            {
                return A;
            }
        }

        public int E => B;

        public int F { get; private set; }

        public int G { get; private set; }

        public int H { get; set; }

        public int I { get; set; }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
