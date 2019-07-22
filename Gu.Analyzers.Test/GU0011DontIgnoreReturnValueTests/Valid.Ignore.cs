// ReSharper disable InconsistentNaming
namespace Gu.Analyzers.Test.GU0011DontIgnoreReturnValueTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static partial class Valid
    {
        internal static class Ignore
        {
            [TestCase("stringBuilder.AppendLine(\"test\");")]
            [TestCase("stringBuilder.Append(\"test\");")]
            [TestCase("stringBuilder.Clear();")]
            public static void StringBuilder(string expression)
            {
                var code = @"
namespace N
{
    using System.Text;

    public class C
    {
        public void M()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(""test"");
        }
    }
}".AssertReplace("stringBuilder.AppendLine(\"test\");", expression);

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void StringBuilderAppendChained()
            {
                var code = @"
namespace N
{
    using System.Text;

    public class C
    {
        public void M()
        {
            var sb = new StringBuilder();
            sb.Append(""1"").Append(""2"");
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void WhenReturningSameInstance()
            {
                var ensure = @"
namespace N
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
                var c = @"
namespace N
{
    public class C
    {
        public C(string text)
        {
            Ensure.NotNull(text, nameof(text));
        }
    }
}";
                RoslynAssert.Valid(Analyzer, ensure, c);
            }

            [Test]
            public static void WhenReturningThis()
            {
                var code = @"
namespace N
{
    public class C
    {
        public C M1()
        {
            return this;
        }

        public void M2()
        {
            M1();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void WhenExtensionMethodReturningThis()
            {
                var c2 = @"
namespace N
{
    internal static class C2
    {
        internal static T Id<T>(this T value)
        {
            return value;
        }
    }
}";
                var c1 = @"
namespace N
{
    public class C1
    {
        private C1()
        {
            var meh = 1;
            meh.Id();
        }
    }
}";
                RoslynAssert.Valid(Analyzer, c2, c1);
            }

            [Explicit("Don't know if we want this.")]
            [TestCase("this.ints.Add(1);")]
            [TestCase("ints.Add(1);")]
            [TestCase("this.ints.Remove(1);")]
            public static void HashSet(string operation)
            {
                var code = @"
namespace N
{
    using System.Collections.Generic;

    public sealed class C
    {
        private readonly HashSet<int> ints = new HashSet<int>();

        public C()
        {
            this.ints.Add(1);
        }
    }
}".AssertReplace("this.ints.Add(1);", operation);

                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("this.ints.Add(1);")]
            [TestCase("ints.Add(1);")]
            [TestCase("this.ints.Remove(1);")]
            public static void IList(string operation)
            {
                var code = @"
namespace N
{
    using System.Collections;
    using System.Collections.Generic;

    public sealed class C
    {
        private readonly IList ints = new List<int>();

        public C()
        {
            this.ints.Add(1);
        }
    }
}".AssertReplace("this.ints.Add(1);", operation);

                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("ints.Add(1);")]
            [TestCase("ints.Remove(1);")]
            [TestCase("ints.RemoveAll(x => x > 2);")]
            public static void ListOfInt(string operation)
            {
                var code = @"
namespace N
{
    using System.Collections.Generic;

    public class C
    {
        public C(List<int> ints)
        {
            ints.RemoveAll(x => x > 2);
        }
    }
}".AssertReplace("ints.RemoveAll(x => x > 2);", operation);

                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("map.TryAdd(1, 1);")]
            public static void ConcurrentDictionary(string operation)
            {
                var code = @"
namespace N
{
    using System.Collections.Concurrent;

    public class C
    {
        public C(ConcurrentDictionary<int, int> map)
        {
            map.TryAdd(1, 1);
        }
    }
}".AssertReplace("map.TryAdd(1, 1);", operation);

                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("mock.Setup(x => x.GetFormat(It.IsAny<Type>())).Returns(null)")]
            public static void MoqSetupReturns(string expression)
            {
                var code = @"
namespace N
{
    using System;
    using Moq;
    using NUnit.Framework;

    public class C
    {
        [Test]
        public void M()
        {
            var mock = new Mock<IFormatProvider>();
            mock.Setup(x => x.GetFormat(It.IsAny<Type>())).Returns(null);
        }
    }
}".AssertReplace("mock.Setup(x => x.GetFormat(It.IsAny<Type>())).Returns(null)", expression);

                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("mock.Setup(x => x.M())")]
            public static void MoqSetupVoid(string setup)
            {
                var code = @"
namespace N
{
    using Moq;

    public class Foo
    {
        public Foo()
        {
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.M());
        }
    }

    public interface IFoo
    {
        void M();
    }
}".AssertReplace("mock.Setup(x => x.M())", setup);

                RoslynAssert.Valid(Analyzer, code);
            }

            [TestCase("this.Bind<C>().To<C>()")]
            [TestCase("this.Bind<C>().To<C>().InSingletonScope()")]
            [TestCase("this.Bind<C>().ToMethod(x => new C())")]
            public static void NinjectFluent(string bind)
            {
                var code = @"
namespace N
{
    using Ninject.Modules;

    public sealed class C : NinjectModule
    {
        public override void Load()
        {
            this.Bind<C>().To<C>();
        }
    }
}".AssertReplace("this.Bind<C>().To<C>()", bind);

                RoslynAssert.Valid(Analyzer, code);
            }

            [Test]
            public static void DocumentEditorExtensionMethod()
            {
                var extCode = @"
namespace N
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
                var c = @"
namespace N
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    internal sealed class C
    {
        public void M(DocumentEditor editor, UsingDirectiveSyntax directive)
        {
            editor.AddUsing(directive);
        }
    }
}";
                RoslynAssert.Valid(Analyzer, extCode, c);
            }
        }
    }
}
