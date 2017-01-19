namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0020SortProperties>
    {
        [Test]
        public async Task WithCustomEvent()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task GetOnlies()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExplicitImplementation()
        {
            var interfaceCode = @"    
    interface IValue
    {
        object Value { get; }
    }";

            var testCode = @"
    public class Foo : IValue
    {
        public int Value { get; } = 5;

        object IValue.Value { get; } = 5;
    }";
            await this.VerifyHappyPathAsync(interfaceCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Mutables()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NotifyingMutables()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task MutablesBySetter()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ExpressionBodies()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Calculated()
        {
            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task InternalBeforePublicIndexer()
        {
            var testCode = @"
public class Foo
{
    internal int Value { get; set; }

    public int this[int index] => index;
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticBeforeInstance()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int b)
        {
            this.B = b;
        }

        public static int A { get; } = 1;

        public int B { get; }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticGetOnlyBeforeCalculated()
        {
            var testCode = @"
    public class Foo
    {
        public static int A { get; } = 1;

        public int B => A;
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task PublicBeforeProtectedStatic()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        protected static int B { get; }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task Realistic()
        {
            var testCode = @"
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
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}