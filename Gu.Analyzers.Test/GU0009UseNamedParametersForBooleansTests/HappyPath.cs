namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0009UseNamedParametersForBooleans>
    {
        [Test]
        public async Task UsesNamedParameter()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task UsingADefaultBooleanParameter()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task NondeducedGenericBooleanParameter()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DeducedGenericBooleanParameter()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task FunctionAcceptingABooleanVariable()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task BooleanParamsArray()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontWarnOnDisposePattern()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontWarnOnAssertAreEqual()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontWarnOnConfigureAwait()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("SetValue")]
        [TestCase("SetCurrentValue")]
        public async Task DontWarnOn(string method)
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
}";
            testCode = testCode.AssertReplace("SetValue", method);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase("textBox.SetBar(true)")]
        [TestCase("Foo.SetBar(textBox, true)")]
        public async Task DontWarnOnAttachedPropertySetter(string method)
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
}";
            testCode = testCode.AssertReplace("textBox.SetBar(true)", method);
            await this.VerifyHappyPathAsync(apCode, testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task DontWarnInExpressionTree()
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
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}