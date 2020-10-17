namespace Gu.Analyzers.Test.Refactorings
{
    using Gu.Analyzers.Refactoring;
    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis.CodeRefactorings;

    using NUnit.Framework;

    [Ignore("RoslynAssert does not handle many.")]
    public static class ParameterRefactoringTests
    {
        private static readonly CodeRefactoringProvider Refactoring = new ParameterRefactoring();

        [Test]
        public static void MoveAssignmentBefore()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly int i;
        private readonly string s;

        public C(↓int i, string s)
        {
            this.s = s;
            this.i = i;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly int i;
        private readonly string s;

        public C(int i, string s)
        {
            this.i = i;
            this.s = s;
        }
    }
}";
            RoslynAssert.Refactoring(Refactoring, before, after, title: "Move assignment to match parameter position.");
        }

        [Test]
        public static void MoveAssignmentAfter()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly int i;
        private readonly string s;

        public C(int i, ↓string s)
        {
            this.s = s;
            this.i = i;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly int i;
        private readonly string s;

        public C(int i, string s)
        {
            this.i = i;
            this.s = s;
        }
    }
}";
            RoslynAssert.Refactoring(Refactoring, before, after, title: "Move assignment to match parameter position.");
        }

        [Test]
        public static void MoveParameterBefore()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly int i;
        private readonly string s;

        public C(string s, ↓int i)
        {
            this.i = i;
            this.s = s;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly int i;
        private readonly string s;

        public C(int i, string s)
        {
            this.i = i;
            this.s = s;
        }
    }
}";
            RoslynAssert.Refactoring(Refactoring, before, after, title: "Move parameter to match assigned member position.");
        }

        [Test]
        public static void MoveParameterAfter()
        {
            var before = @"
namespace N
{
    public class C
    {
        private readonly int i;
        private readonly string s;

        public C(↓string s, int i)
        {
            this.i = i;
            this.s = s;
        }
    }
}";

            var after = @"
namespace N
{
    public class C
    {
        private readonly int i;
        private readonly string s;

        public C(int i, string s)
        {
            this.i = i;
            this.s = s;
        }
    }
}";
            RoslynAssert.Refactoring(Refactoring, before, after, title: "Move parameter to match assigned member position.");
        }
    }
}
