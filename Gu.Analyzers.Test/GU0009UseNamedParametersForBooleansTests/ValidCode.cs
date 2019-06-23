namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly GU0009UseNamedParametersForBooleans Analyzer = new GU0009UseNamedParametersForBooleans();

        [Test]
        public void UsesNamedParameter()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void UsingADefaultBooleanParameter()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void NondeducedGenericBooleanParameter()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DeducedGenericBooleanParameter()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void FunctionAcceptingABooleanVariable()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void BooleanParamsArray()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DontWarnOnDisposePattern()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DontWarnOnAssertAreEqual()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void DontWarnOnConfigureAwait()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("SetValue")]
        [TestCase("SetCurrentValue")]
        public void DontWarnOn(string method)
        {
            var testCode = @"
namespace RoslynSandbox
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

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("textBox.SetBar(true)")]
        [TestCase("Foo.SetBar(textBox, true)")]
        public void DontWarnOnAttachedPropertySetter(string method)
        {
            var apCode = @"
namespace RoslynSandbox
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
namespace RoslynSandbox
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
        public void DontWarnInExpressionTree()
        {
            var testCode = @"
namespace RoslynSandbox
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
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
