namespace Gu.Analyzers.Test.GU0100WrongDocsTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DocsAnalyzer();
        private static readonly CodeFixProvider Fix = new DocsFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0100WrongDocs);

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

        public event PropertyChangedEventHandler PropertyChanged;

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

    public class ViewModel : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

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
        /// <param name=""propertyName"">The <see cref=""string""/>.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
