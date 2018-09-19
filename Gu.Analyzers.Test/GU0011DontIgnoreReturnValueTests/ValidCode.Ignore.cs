// ReSharper disable InconsistentNaming
namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal partial class ValidCode
    {
        internal class Ignore
        {
            [TestCase("stringBuilder.AppendLine(\"test\");")]
            [TestCase("stringBuilder.Append(\"test\");")]
            [TestCase("stringBuilder.Clear();")]
            public void StringBuilder(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        public void Bar()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(""test"");
        }
    }
}";
                testCode = testCode.AssertReplace("stringBuilder.AppendLine(\"test\");", code);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void StringBuilderAppendChained()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        public void Bar()
        {
            var sb = new StringBuilder();
            sb.Append(""1"").Append(""2"");
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void WhenReturningSameInstance()
            {
                var ensureCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public static class Ensure
    {
        public static T NotNull<T>(T value, string parameter, [CallerMemberName] string caller = null)
            where T : class
        {
            Debug.Assert(!string.IsNullOrEmpty(parameter), ""parameter cannot be null"");

            if (value == null)
            {
                var message = $""Expected parameter {parameter} in member {caller} to not be null"";
                throw new ArgumentNullException(parameter, message);
            }

            return value;
        }

        public static T NotNull<T>(T? value, string parameter, [CallerMemberName] string caller = null)
            where T : struct
        {
            Debug.Assert(!string.IsNullOrEmpty(parameter), ""parameter cannot be null"");

            if (value == null)
            {
                var message = $""Expected parameter {parameter} in member {caller} to not be null"";
                throw new ArgumentNullException(parameter, message);
            }

            return value.Value;
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(string text)
        {
            Ensure.NotNull(text, nameof(text));
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, ensureCode, testCode);
            }

            [Test]
            public void WhenReturningThis()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo Bar()
        {
            return this;
        }

        public void Meh()
        {
            Bar();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void WhenExtensionMethodReturningThis()
            {
                var barCode = @"
namespace RoslynSandbox
{
    internal static class Bar
    {
        internal static T Id<T>(this T value)
        {
            return value;
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private Foo()
        {
            var meh =1;
            meh.Id();
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, barCode, testCode);
            }

            [Explicit("Don't know if we want this.")]
            [TestCase("this.ints.Add(1);")]
            [TestCase("ints.Add(1);")]
            [TestCase("this.ints.Remove(1);")]
            public void HashSet(string operation)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public sealed class Foo
    {
        private readonly HashSet<int> ints = new HashSet<int>();

        public Foo()
        {
            this.ints.Add(1);
        }
    }
}";
                testCode = testCode.AssertReplace("this.ints.Add(1);", operation);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [TestCase("this.ints.Add(1);")]
            [TestCase("ints.Add(1);")]
            [TestCase("this.ints.Remove(1);")]
            public void IList(string operation)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections;
    using System.Collections.Generic;

    public sealed class Foo
    {
        private readonly IList ints = new List<int>();

        public Foo()
        {
            this.ints.Add(1);
        }
    }
}";
                testCode = testCode.AssertReplace("this.ints.Add(1);", operation);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [TestCase("ints.Add(1);")]
            [TestCase("ints.Remove(1);")]
            [TestCase("ints.RemoveAll(x => x > 2);")]
            public void ListOfInt(string operation)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public class Foo
    {
        public Foo(List<int> ints)
        {
            ints.RemoveAll(x => x > 2);
        }
    }
}";
                testCode = testCode.AssertReplace("ints.RemoveAll(x => x > 2);", operation);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [TestCase("map.TryAdd(1, 1);")]
            public void ConcurrentDictionary(string operation)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Concurrent;

    public class Foo
    {
        public Foo(ConcurrentDictionary<int, int> map)
        {
            map.TryAdd(1, 1);
        }
    }
}";
                testCode = testCode.AssertReplace("map.TryAdd(1, 1);", operation);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [TestCase("mock.Setup(x => x.GetFormat(It.IsAny<Type>())).Returns(null)")]
            public void MoqSetupReturns(string code)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using Moq;
    using NUnit.Framework;

    public class Foo
    {
        [Test]
        public void Test()
        {
            var mock = new Mock<IFormatProvider>();
            mock.Setup(x => x.GetFormat(It.IsAny<Type>())).Returns(null);
        }
    }
}";
                testCode = testCode.AssertReplace("mock.Setup(x => x.GetFormat(It.IsAny<Type>())).Returns(null)", code);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [TestCase("mock.Setup(x => x.Bar())")]
            public void MoqSetupVoid(string setup)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using Moq;

    public class Foo
    {
        public Foo()
        {
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.Bar());
        }
    }

    public interface IFoo
    {
        void Bar();
    }
}";
                testCode = testCode.AssertReplace("mock.Setup(x => x.Bar())", setup);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [TestCase("this.Bind<Foo>().To<Foo>()")]
            [TestCase("this.Bind<Foo>().To<Foo>().InSingletonScope()")]
            [TestCase("this.Bind<Foo>().ToMethod(x => new Foo())")]
            public void NinjectFluent(string bind)
            {
                var testCode = @"
namespace RoslynSandbox
{
    using Ninject.Modules;

    public sealed class Foo : NinjectModule
    {
        public override void Load()
        {
            this.Bind<Foo>().To<Foo>();
        }
    }
}";
                testCode = testCode.AssertReplace("this.Bind<Foo>().To<Foo>()", bind);
                AnalyzerAssert.Valid(Analyzer, testCode);
            }

            [Test]
            public void DocumentEditorExtensionMethod()
            {
                var extCode = @"
namespace RoslynSandbox
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    public static class DocumentEditorExt
    {
        internal static DocumentEditor AddUsing(this DocumentEditor editor, UsingDirectiveSyntax usingDirective)
        {
            editor.ReplaceNode(
                editor.OriginalRoot,
                (root, _) => editor.OriginalRoot);

            return editor;
        }
    }
}";
                var testCode = @"
namespace RoslynSandbox
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal sealed class Foo
    {
        public void Bar(DocumentEditor editor, UsingDirectiveSyntax directive)
        {
            editor.AddUsing(directive);
        }
    }
}";
                AnalyzerAssert.Valid(Analyzer, extCode, testCode);
            }
        }
    }
}
