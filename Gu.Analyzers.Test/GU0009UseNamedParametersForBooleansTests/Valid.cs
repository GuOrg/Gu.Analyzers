namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly ArgumentAnalyzer Analyzer = new();

    [Test]
    public static void UsesNamedParameter()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void Floof(int howMuch, bool useFluffyBuns)
        {
        
        }

        public void Another()
        {
            Floof(42, useFluffyBuns: false);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void UsingADefaultBooleanParameter()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void Floof(int howMuch, bool useFluffyBuns = true)
        {
        
        }

        public void Another()
        {
            Floof(42);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void NonDeducedGenericBooleanParameter()
    {
        var code = @"
namespace N
{
    using System.Collections.Generic;

    public class C
    {
        public void Another()
        {
            var a = new List<bool>();
            a.Add(true);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DeducedGenericBooleanParameter()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public void Another()
        {
            var a = Tuple.Create(true, false);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void FunctionAcceptingABooleanVariable()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void Floof(int howMuch, bool useFluffyBuns)
        {
        
        }

        public void Another(bool useFluffyBuns)
        {
            Floof(42, useFluffyBuns);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void BooleanParamsArray()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void Floof(params bool[] useFluffyBuns)
        {
        
        }

        public void Another()
        {
            Floof(true, true, true, false, true, false);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DoNotWarnOnDisposePattern()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public abstract class CBase : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed = false;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                this.stream.Dispose();
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void DoNotWarnOnAssertAreEqual()
    {
        var code = @"
namespace N
{
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void M()
        {
            Assert.AreEqual(true, true);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("Task")]
    [TestCase("Task<int>")]
    public static void DoNotWarnOnConfigureAwait(string expression)
    {
        var code = @"
namespace N
{
    using System.Threading.Tasks;

    public class C
    {
        public void M(Task<int> task)
        {
            task.ConfigureAwait(false);
            task.ConfigureAwait(true);
        }
    }
}".AssertReplace("Task<int>", expression);
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("SetValue")]
    [TestCase("SetCurrentValue")]
    public static void DoNotWarnOn(string method)
    {
        var code = @"
namespace N
{
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    public class C
    {
        public void M()
        {
            var textBox = new TextBox();
            textBox.SetValue(TextBoxBase.IsReadOnlyProperty, true);
        }
    }
}".AssertReplace("SetValue", method);

        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("textBox.SetM(true)")]
    [TestCase("C.SetM(textBox, true)")]
    public static void DoNotWarnOnAttachedPropertySetter(string method)
    {
        var c = @"
namespace N
{
    using System.Windows;

    public static class C
    {
        public static readonly DependencyProperty MProperty = DependencyProperty.RegisterAttached(
            ""M"",
            typeof(bool),
            typeof(C),
            new PropertyMetadata(default(bool)));

        public static void SetM(this DependencyObject element, bool value)
        {
            element.SetValue(MProperty, value);
        }

        public static bool GetM(DependencyObject element)
        {
            return (bool)element.GetValue(MProperty);
        }
    }
}";
        var testCode = @"
namespace N
{
    using System.Windows.Controls;

    public class Baz
    {
        public void Meh()
        {
            var textBox = new TextBox();
            textBox.SetM(true);
        }
    }
}".AssertReplace("textBox.SetM(true)", method);

        RoslynAssert.Valid(Analyzer, c, testCode);
    }

    [Test]
    public static void DoNotWarnInExpressionTree()
    {
        var code = @"
namespace N
{
    using System;
    using System.Linq.Expressions;

    internal class C
    {
        public void M()
        {
            Meh(() => Id(true)); // GU0009 should not warn here
        }

        internal static void Meh(Expression<Action> meh)
        {
        }

        private static bool Id(bool self) => self;
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void SchedulerOperationConfigureAwait()
    {
        var code = @"
namespace N
{
    using System;
    using System.Reactive.Concurrency;
    using System.Threading.Tasks;

    internal class C
    {
        internal static async Task SleepAsync(IScheduler scheduler, TimeSpan dueTime)
        {
            await scheduler.Sleep(dueTime).ConfigureAwait(false);
        }
    }
}
";
        RoslynAssert.Valid(Analyzer, code);
    }
}
