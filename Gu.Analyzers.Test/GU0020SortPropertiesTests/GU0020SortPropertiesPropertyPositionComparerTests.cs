namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    internal class GU0020SortPropertiesPropertyPositionComparerTests
    {
        [TestCase("A", "A", 0)]
        [TestCase("A", "B", -1)]
        [TestCase("A", "D", -1)]
        [TestCase("A", "E", -1)]
        [TestCase("A", "F", -1)]
        [TestCase("A", "G", -1)]
        [TestCase("A", "H", -1)]
        [TestCase("A", "I", -1)]
        [TestCase("A", "J", -1)]
        [TestCase("A", "K", -1)]
        [TestCase("A", "this[int index]", -1)]
        [TestCase("B", "int C", 0)]
        [TestCase("B", "D", -1)]
        [TestCase("B", "E", -1)]
        [TestCase("B", "F", -1)]
        [TestCase("B", "G", -1)]
        [TestCase("B", "H", -1)]
        [TestCase("B", "I", -1)]
        [TestCase("B", "J", -1)]
        [TestCase("B", "K", -1)]
        [TestCase("B", "this[int index]", -1)]
        [TestCase("D", "E", -1)]
        [TestCase("D", "F", -1)]
        [TestCase("D", "G", -1)]
        [TestCase("D", "H", -1)]
        [TestCase("D", "I", -1)]
        [TestCase("D", "J", -1)]
        [TestCase("D", "K", -1)]
        [TestCase("D", "this[int index]", -1)]
        [TestCase("E", "F", -1)]
        [TestCase("E", "G", -1)]
        [TestCase("E", "H", -1)]
        [TestCase("E", "I", -1)]
        [TestCase("E", "J", -1)]
        [TestCase("E", "K", -1)]
        [TestCase("E", "this[int index]", -1)]
        [TestCase("F", "G", -1)]
        [TestCase("F", "H", -1)]
        [TestCase("F", "I", -1)]
        [TestCase("F", "J", -1)]
        [TestCase("F", "K", -1)]
        [TestCase("F", "this[int index]", -1)]
        [TestCase("G", "H", -1)]
        [TestCase("G", "I", -1)]
        [TestCase("G", "J", -1)]
        [TestCase("G", "K", -1)]
        [TestCase("G", "this[int index]", -1)]
        [TestCase("H", "I", -1)]
        [TestCase("H", "J", -1)]
        [TestCase("H", "K", -1)]
        [TestCase("H", "this[int index]", -1)]
        [TestCase("I", "J", -1)]
        [TestCase("I", "K", -1)]
        [TestCase("I", "this[int index]", -1)]
        [TestCase("J", "K", -1)]
        [TestCase("J", "this[int index]", -1)]
        public void Compare(string x, string y, int expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
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
            var first = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>(x);
            var other = syntaxTree.FindBestMatch<BasePropertyDeclarationSyntax>(y);
            Assert.AreEqual(expected, GU0020SortProperties.PropertyPositionComparer.Default.Compare(first, other));
            Assert.AreEqual(-1 * expected, GU0020SortProperties.PropertyPositionComparer.Default.Compare(other, first));
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
            Assert.AreEqual(0, GU0020SortProperties.PropertyPositionComparer.Default.Compare(first, other));
            Assert.AreEqual(0, GU0020SortProperties.PropertyPositionComparer.Default.Compare(other, first));
        }
    }
}