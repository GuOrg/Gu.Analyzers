namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0020SortProperties>
    {
        private static readonly GU0020SortProperties Analyzer = new GU0020SortProperties();

        [Test]
        public void WithCustomEvent()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void GetOnlies()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ExplicitImplementationGetOnly()
        {
            var interfaceCode = @"
namespace RoslynSandbox
{
    interface IValue
    {
        object Value { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    public class Foo : IValue
    {
        public int Value { get; } = 5;

        object IValue.Value { get; } = 5;
    }
}";
            AnalyzerAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public void ExplicitImplementationGetOnlyIndexer()
        {
            var interfaceCode = @"
namespace RoslynSandbox
{
    interface IValue
    {
        object this[int index] { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    public class Foo : IValue
    {
        public int this[int index] => index;

        object IValue.this[int index] => index;
    }
}";
            AnalyzerAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public void ExplicitGetSetIndexerAndGetOnlyIndexer()
        {
            var interfaceCode = @"
namespace RoslynSandbox
{
    interface IValue
    {
        int this[int index] { get; set; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    public class Foo : IValue
    {
        public int this[int index] => index;

        int IValue.this[int index]
        {
            get { return this[index]; }
            set { return; }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public void ExplicitGetSetIndexerAndGetSetIndexer()
        {
            var interfaceCode = @"
namespace RoslynSandbox
{
    interface IValue
    {
        int this[int index] { get; set; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    public class Foo : IValue
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
            AnalyzerAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public void ExplicitImplementationCalculatedBeforeGetSet()
        {
            var interfaceCode = @"
namespace RoslynSandbox
{
    interface IValue
    {
        object Value { get; }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    public class Foo : IValue
    {
        object IValue.Value => this.Value;

        public int Value { get; set; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, interfaceCode, testCode);
        }

        [Test]
        public void Mutables()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void NotifyingMutables()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void MutablesBySetter()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ExpressionBodies()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void Calculated()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(StringComparison stringComparison)
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void InternalBeforePublicIndexer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        internal int Value { get; set; }

        public int this[int index] => index;
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticBeforeInstance()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int b)
        {
            this.B = b;
        }

        public static int A { get; } = 1;

        public int B { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void StaticGetOnlyBeforeCalculated()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static int A { get; } = 1;

        public int B => A;
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PublicBeforeProtectedStatic()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        protected static int B { get; }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void PublicInitializedWithProtectedStatic()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        protected static int B { get; }

        public static int A { get; } = B;
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void Realistic()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}