namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class ValidCode
    {
        private static readonly GU0009UseNamedParametersForBooleans Analyzer = new GU0009UseNamedParametersForBooleans();

        [Test]
        public static void UsesNamedParameter()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class Foo
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
    using System;
    using System.Collections.Generic;

    public class Foo
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
    using System;
    using System.Collections.Generic;

    public class Foo
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
    using System.Collections.Generic;

    public class Foo
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
    using System;
    using System.Collections.Generic;

    public class Foo
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
    using System;
    using System.Collections.Generic;

    public class Foo
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

    public abstract class FooBase : IDisposable
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

    public class FooTests
    {
        [Test]
        public void Bar()
        {
            Assert.AreEqual(true, true);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void DoNotWarnOnConfigureAwait()
        {
            var code = @"
namespace N
{
    using System.Threading.Tasks;

    public class FooTests
    {
        public void Bar()
        {
            Task<int> a = null;
            Task b = null;
            a.ConfigureAwait(false);
            a.ConfigureAwait(true);
            b.ConfigureAwait(false);
            b.ConfigureAwait(true);
        }
    }
}";
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

    public class Foo
    {
        public void Meh()
        {
            var textBox = new TextBox();
            textBox.SetValue(TextBoxBase.IsReadOnlyProperty, true);
        }
    }
}".AssertReplace("SetValue", method);

            RoslynAssert.Valid(Analyzer, code);
        }

        [TestCase("textBox.SetBar(true)")]
        [TestCase("Foo.SetBar(textBox, true)")]
        public static void DoNotWarnOnAttachedPropertySetter(string method)
        {
            var apCode = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(bool),
            typeof(Foo),
            new PropertyMetadata(default(bool)));

        public static void SetBar(this DependencyObject element, bool value)
        {
            element.SetValue(BarProperty, value);
        }

        public static bool GetBar(DependencyObject element)
        {
            return (bool)element.GetValue(BarProperty);
        }
    }
}";
            var testCode = @"
namespace N
{
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    public class Baz
    {
        public void Meh()
        {
            var textBox = new TextBox();
            textBox.SetBar(true);
        }
    }
}".AssertReplace("textBox.SetBar(true)", method);

            RoslynAssert.Valid(Analyzer, apCode, testCode);
        }

        [Test]
        public static void DoNotWarnInExpressionTree()
        {
            var code = @"
namespace N
{
    using System;
    using System.Linq.Expressions;

    internal class Foo
    {
        public void Bar()
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
    }
}
