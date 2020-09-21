namespace Gu.Analyzers.Test.Refactorings
{
    using Gu.Analyzers.Refactoring;
    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis.CodeRefactorings;

    using NUnit.Framework;

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
    }
}
