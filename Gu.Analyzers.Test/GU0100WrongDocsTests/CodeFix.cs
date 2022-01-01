namespace Gu.Analyzers.Test.GU0100WrongDocsTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class CodeFix
{
    private static readonly DocsAnalyzer Analyzer = new();
    private static readonly DocsFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0100WrongCrefType);

    [TestCase("string")]
    [TestCase("System.String")]
    [TestCase("Decoder")]
    public static void WhenWrong(string type)
    {
        var before = @"
namespace N
{
    using System.Text;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""builder"">The <see cref=""↓string""/>.</param>
        public void M(StringBuilder builder)
        {
        }
    }
}".AssertReplace("string", type);
        var after = @"
namespace N
{
    using System.Text;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public void M(StringBuilder builder)
        {
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void WhenWrongArray()
    {
        var before = @"
namespace N
{
    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""array"">The <see cref=""↓string""/>.</param>
        public void M(string[] array)
        {
        }
    }
}";
        var after = @"
namespace N
{
    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""array"">The <see cref=""string[]""/>.</param>
        public void M(string[] array)
        {
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void WhenWrongGenericList()
    {
        var before = @"
namespace N
{
    using System.Collections.Generic;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""list"">The <see cref=""↓string""/>.</param>
        public void M(List<string> list)
        {
        }
    }
}";
        var after = @"
namespace N
{
    using System.Collections.Generic;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""list"">The <see cref=""List{string}""/>.</param>
        public void M(List<string> list)
        {
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Test]
    public static void OnPropertyChanged()
    {
        var before = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ViewModel : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Square => this.value * this.value;

        public int Value
        {
            get => this.value;
            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.Square));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""propertyName"">The <see cref=""↓int""/>.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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

    public class ViewModel : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Square => this.value * this.value;

        public int Value
        {
            get => this.value;
            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.Square));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name=""propertyName"">The <see cref=""string?""/>.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [TestCase("KeyValuePair{int, StringBuilder}")]
    [TestCase("KeyValuePair{Type, int}")]
    public static void WhenKeyValuePair(string cref)
    {
        var before = @"
namespace N
{
    using System.Text;
    using System.Collections.Generic;
    using System;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""kvp"">The <see cref=""KeyValuePair{Type, StringBuilder}""/>.</param>
        public void M(KeyValuePair<Type, StringBuilder> kvp)
        {
        }
    }
}".AssertReplace("KeyValuePair{Type, StringBuilder}", cref);
        var after = @"
namespace N
{
    using System.Text;
    using System.Collections.Generic;
    using System;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""kvp"">The <see cref=""KeyValuePair{Type, StringBuilder}""/>.</param>
        public void M(KeyValuePair<Type, StringBuilder> kvp)
        {
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }
}