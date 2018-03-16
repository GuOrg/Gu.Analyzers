namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal class GU0020SortPropertiesPropertyPositionComparerTests
    {
        private static readonly SyntaxTree SyntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        public static int A { get; }

        public int B { get; }

        public int C { get; }

        public int D => C;

        public int E { get; private set; }

        public int F { get; internal set; }

        public int G { get; set; }

        internal int H { get; }

        protected static int I { get; }

        protected int J { get; }

        private int K { get; }

        public int this[int index]
        {
            get { return 0; }
            set { }
        }
    }
}");

        private static readonly IReadOnlyList<TestCaseData> TestCaseSource = CreateTestCases().ToArray();

        [TestCaseSource(nameof(TestCaseSource))]
        public void Compare(BasePropertyDeclarationSyntax x, BasePropertyDeclarationSyntax y)
        {
            var comparer = GU0020SortProperties.PropertyPositionComparer.Default;
            Assert.AreEqual(-1, comparer.Compare(x, y));
            Assert.AreEqual(1, comparer.Compare(y, x));
            Assert.AreEqual(0, comparer.Compare(x, x));
            Assert.AreEqual(0, comparer.Compare(y, y));
        }

        [Test]
        public void PublicInitializedWithPrivateStatic()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        private static int A { get; }

        public static int B { get; } = A;
    }
}");
            var first = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("int A");
            var other = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("int B");
            Assert.AreEqual(-1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(first, other));
            Assert.AreEqual(1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(other, first));
        }

        [Test]
        public void PublicInitializedWithPrivateStaticExplicitType()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Foo
    {
        private static int A { get; }

        public static int B { get; } = Foo.A;
    }
}");
            var first = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("int A");
            var other = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("int B");
            Assert.AreEqual(-1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(first, other));
            Assert.AreEqual(1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(other, first));
        }

        [Test]
        public void PublicInitializedWithOtherClass()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public class Bar
    {
        public static int B { get; }
    }

    public class Foo
    {
        public static int A { get; } = Bar.B;

        private static int B { get; }
    }
}");
            var first = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("public static int A { get; }");
            var other = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("private static int B { get; }");
            Assert.AreEqual(-1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(first, other));
            Assert.AreEqual(1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(other, first));
        }

        [TestCase("int A", "IFoo.B", -1)]
        [TestCase("IFoo.B", "IFoo.B", 0)]
        [TestCase("IFoo.B", "C", -1)]
        public void ExplicitInterfaceIsPublic(string x, string y, int expected)
        {
            // Cheating a bit here and assuming public interface.
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    interface IFoo
    {
        object B { get; }
    }

    public class Foo : IFoo
    {
        public int A { get; }

        object IFoo.B => this.A;

        public int C { get; set; }
    }
}");
            var first = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>(x);
            var other = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>(y);
            Assert.AreEqual(expected, GU0020SortProperties.PropertyPositionComparer.Default.Compare(first, other));
            Assert.AreEqual(-1 * expected, GU0020SortProperties.PropertyPositionComparer.Default.Compare(other, first));
        }

        [Test]
        public void ExplicitInterfaceIndexerAndPrivate()
        {
            // Cheating a bit here and assuming public interface.
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public interface IValue
    {
        int this[int index] { get; set; }
    }

    public class Foo : IValue
    {
        private int meh;

        int IValue.this[int index]
        {
            get { return this.meh; }
            set { this.meh = index; }
        }

        private int this[int index]
        {
            get { return this.meh; }
            set { this.meh = index; }
        }
    }
}");
            var first = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("int IValue.this[int index]");
            var other = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("private int this[int index]");
            Assert.AreEqual(-1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(first, other));
            Assert.AreEqual(1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(other, first));
        }

        [Test]
        public void ExplicitInterfaceIndexerAndPublic()
        {
            // Cheating a bit here and assuming public interface.
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    public interface IValue
    {
        int this[int index] { get; set; }
    }

    public class Foo : IValue
    {
        private int meh;

        int IValue.this[int index]
        {
            get { return this.meh; }
            set { this.meh = index; }
        }

        public int this[int index]
        {
            get { return this.meh; }
            set { this.meh = index; }
        }
    }
}");
            var first = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("int IValue.this[int index]");
            var other = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>("public int this[int index]");
            Assert.AreEqual(-1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(first, other));
            Assert.AreEqual(1, GU0020SortProperties.PropertyPositionComparer.Default.Compare(other, first));
        }

        private static IEnumerable<TestCaseData> CreateTestCases()
        {
            var foo = SyntaxTree.FindClassDeclaration("Foo");
            foreach (var member1 in foo.Members)
            {
                foreach (var member2 in foo.Members)
                {
                    if (member1.SpanStart < member2.SpanStart)
                    {
                        yield return new TestCaseData(member1, member2);
                    }
                }
            }
        }
    }
}