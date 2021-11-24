namespace Gu.Analyzers.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class HandlesRecursion
    {
        private static readonly IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers =
            typeof(Descriptors)
                .Assembly
                .GetTypes()
                .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
                .ToArray();

        [Test]
        public static void NotEmpty()
        {
            CollectionAssert.IsNotEmpty(AllAnalyzers);
            Assert.Pass($"Count: {AllAnalyzers.Count}");
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static void CtorCallingSelf(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace N
{
    internal abstract class C
    {
        internal C()
            : this()
        {
        }
    }
}";
            var sln = CodeFactory.CreateSolution(testCode, CodeFactory.DefaultCompilationOptions(analyzer), MetadataReferences.FromAttributes());
            var diagnostics = Analyze.GetDiagnostics(analyzer, sln);
            RoslynAssert.NoDiagnostics(diagnostics);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static void RecursiveSample(DiagnosticAnalyzer analyzer)
        {
            var c = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    internal abstract class C
    {
        internal C()
        {
#pragma warning disable GU0015 // Don't assign same more than once
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(1);
            // value = value;
#pragma warning restore GU0015 // Don't assign same more than once
        }

        internal int RecursiveExpressionBodyProperty => this.RecursiveExpressionBodyProperty;

        internal int RecursiveStatementBodyProperty
        {
            get
            {
                return this.RecursiveStatementBodyProperty;
            }
        }

        internal int RecursiveExpressionBodyMethod() => this.RecursiveExpressionBodyMethod();

        internal int RecursiveExpressionBodyMethod(int value) => this.RecursiveExpressionBodyMethod(value);

        internal int RecursiveStatementBodyMethod()
        {
            return this.RecursiveStatementBodyMethod();
        }

        internal int RecursiveStatementBodyMethod(int value)
        {
            return this.RecursiveStatementBodyMethod(value);
        }

        internal void Meh()
        {
#pragma warning disable GU0015 // Don't assign same more than once
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(1);
            // value = value;
#pragma warning restore GU0015 // Don't assign same more than once
        }

        private static IReadOnlyList<IDisposable> Flatten(IReadOnlyList<IDisposable> source, List<IDisposable> result = null)
        {
            result = result ?? new List<IDisposable>();
            result.AddRange(source);
            foreach (var condition in source)
            {
                Flatten(new[] { condition }, result);
            }

            return result;
        }

        private static int RecursiveStatementBodyMethodWithOptionalParameter(int value, IEnumerable<int> values = null)
        {
            if (values is null)
            {
                return RecursiveStatementBodyMethodWithOptionalParameter(value, new[] { value });
            }

            return value;
        }
     }
}";
            var validationErrorToStringConverter = @"
namespace N
{
    using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    internal sealed class ValidationErrorToStringConverter : IValueConverter
    {
        /// <summary> Gets the default instance </summary>
        internal static readonly ValidationErrorToStringConverter Default = new ValidationErrorToStringConverter();

#pragma warning disable GU0012
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text;
            }

            if (value is ValidationResult result)
            {
                return this.Convert(result.ErrorContent, targetType, parameter, culture);
            }

            if (value is ValidationError error)
            {
                return this.Convert(error.ErrorContent, targetType, parameter, culture);
            }

            return value;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($""{this.GetType().Name} only supports one-way conversion."");
        }
#pragma warning restore GU0012
    }
}";
            RoslynAssert.Valid(analyzer, c, validationErrorToStringConverter);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static async Task InSetAndRaise(DiagnosticAnalyzer analyzer)
        {
            var viewModelBaseCode = @"
namespace N.Core
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    internal abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool TrySet<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            return this.TrySet(ref field, newValue, propertyName);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";

            var testCode = @"
namespace N.Client
{
    internal class Foo : N.Core.ViewModelBase
    {
        private int value2;

        public int Value1 { get; set; }

        public int Value2
        {
            get { return this.value2; }
            set { this.TrySet(ref this.value2, value); }
        }
    }
}";
            await Analyze.GetDiagnosticsAsync(analyzer, new[] { viewModelBaseCode, testCode }, MetadataReferences.FromAttributes()).ConfigureAwait(false);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static async Task InOnPropertyChanged(DiagnosticAnalyzer analyzer)
        {
            var viewModelBaseCode = @"
namespace N.Core
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    internal abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.OnPropertyChanged(propertyName);
        }
    }
}";

            var testCode = @"
namespace N.Client
{
    internal class Foo : N.Core.ViewModelBase
    {
        private int value2;

        public int Value1 { get; set; }

        public int Value2
        {
            get
            {
                return this.value2;
            }

            set
            {
                if (value == this.value2)
                {
                    return;
                }

                this.value2 = value;
                this.OnPropertyChanged();
            }
        }
    }
}";
            await Analyze.GetDiagnosticsAsync(analyzer, new[] { viewModelBaseCode, testCode }, MetadataReferences.FromAttributes()).ConfigureAwait(false);
        }

        [TestCaseSource(nameof(AllAnalyzers))]
        public static async Task InProperty(DiagnosticAnalyzer analyzer)
        {
            var testCode = @"
namespace N
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    internal class Foo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Value1 => this.Value1;

        public int Value2 => Value2;

        public int Value3 => this.Value1;

        public int Value4
        {
            get
            {
                return this.Value4;
            }

            set
            {
                if (value == this.Value4)
                {
                    return;
                }

                this.Value4 = value;
                this.OnPropertyChanged();
            }
        }

        public int Value5
        {
            get => this.Value5;
            set
            {
                if (value == this.Value5)
                {
                    return;
                }

                this.Value5 = value;
                this.OnPropertyChanged();
            }
        }

        public int Value6
        {
            get => this.Value5;
            set
            {
                if (value == this.Value5)
                {
                    return;
                }

                this.Value5 = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}";
            await Analyze.GetDiagnosticsAsync(analyzer, new[] { testCode }, MetadataReferences.FromAttributes()).ConfigureAwait(false);
        }
    }
}
