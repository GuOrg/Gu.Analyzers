namespace Gu.Analyzers.Test.GU0006UseNameofTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly GU0006UseNameof Analyzer = new GU0006UseNameof();
        private static readonly UseNameofFix Fix = new UseNameofFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0006UseNameof.DiagnosticId);

        [Test]
        public static void WhenThrowingArgumentException()
        {
            var before = @"
namespace N
{
    using System;

    public class C
    {
        public void Meh(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(↓""value"");
            }
        }
    }
}";

            var after = @"
namespace N
{
    using System;

    public class C
    {
        public void Meh(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenThrowingArgumentOutOfRangeException()
        {
            var before = @"
namespace N
{
    using System;

    public class C
    {
        public void Meh(StringComparison value)
        {
            switch (value)
            {
                default:
                    throw new ArgumentOutOfRangeException(↓""value"", value, null);
            }
        }
    }
}";

            var after = @"
namespace N
{
    using System;

    public class C
    {
        public void Meh(StringComparison value)
        {
            switch (value)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenRaisingPropertyChanged()
        {
            var before = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Squared => this.Value*this.Value;

        public int Value
        {
            get
            {
                return this.value;
            }

            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(↓""Squared"");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

            var after = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Squared => this.Value*this.Value;

        public int Value
        {
            get
            {
                return this.value;
            }

            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.Squared));
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenRaisingStaticPropertyChanged()
        {
            var before = @"
namespace N
{
    using System;
    using System.ComponentModel;

    public static class C
    {
        private static string name;

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        public static string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (value == name)
                {
                    return;
                }

                name = value;
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(↓""Name""));
            }
        }
    }
}";

            var after = @"
namespace N
{
    using System;
    using System.ComponentModel;

    public static class C
    {
        private static string name;

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        public static string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (value == name)
                {
                    return;
                }

                name = value;
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenRaisingStaticPropertyChanged2()
        {
            var before = @"
namespace N
{
    using System;
    using System.ComponentModel;

    public class C
    {
        private static string name;
        private int value;

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        public static string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (value == name)
                {
                    return;
                }

                name = value;
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(↓""Name""));
            }
        }

        public int Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
            }
        }
    }
}";

            var after = @"
namespace N
{
    using System;
    using System.ComponentModel;

    public class C
    {
        private static string name;
        private int value;

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        public static string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (value == name)
                {
                    return;
                }

                name = value;
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public int Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
            }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenStaticNameofInstance()
        {
            var before = @"
namespace N
{
    public class C
    {
        public int Value { get; set; }

        public static void Bar()
        {
            Bar(↓""Value"");
        }

        public static void Bar(string meh)
        {
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public int Value { get; set; }

        public static void Bar()
        {
            Bar(nameof(Value));
        }

        public static void Bar(string meh)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenStaticNameofInstance2()
        {
            var before = @"
namespace N
{
    public class C
    {
        public static readonly string Name = Bar(↓""Value"");

        public int Value { get; set; }

        public static string Bar(string meh) => meh;
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public static readonly string Name = Bar(nameof(Value));

        public int Value { get; set; }

        public static string Bar(string meh) => meh;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenStaticNameofInstance3()
        {
            var before = @"
namespace N
{
    public class C
    {
        public static readonly string Name = string.Format(↓""Value"");

        private int value;

        public int Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        public static readonly string Name = string.Format(nameof(Value));

        private int value;

        public int Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void WhenRaisingPropertyChangedUnderscoreNames()
        {
            var before = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int _value;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Squared => _value*_value;

        public int Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (value == _value)
                {
                    return;
                }

                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(↓""Squared"");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

            var after = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class C : INotifyPropertyChanged
    {
        private int _value;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Squared => _value*_value;

        public int Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (value == _value)
                {
                    return;
                }

                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Squared));
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void DependencyProperty()
        {
            var before = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class CControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ↓""Bar"",
            typeof(int),
            typeof(CControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)GetValue(BarProperty); }
            set { SetValue(BarProperty, value); }
        }
    }
}";

            var after = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public class CControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            nameof(Bar),
            typeof(int),
            typeof(CControl),
            new PropertyMetadata(default(int)));

        public int Bar
        {
            get { return (int)GetValue(BarProperty); }
            set { SetValue(BarProperty, value); }
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
