namespace Gu.Analyzers.Test.Refactorings
{
    using Gu.Analyzers.Refactoring;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using NUnit.Framework;

    internal static class SplitStringRefactoringTests
    {
        private static readonly CodeRefactoringProvider Refactoring = new SplitStringRefactoring();

        [Test]
        public static void StartingWithNewLine()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""↓\na"";
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""\n"" +
                       ""a"";
        }
    }
}";
            RoslynAssert.Refactoring(Refactoring, before, after);
        }

        [Test]
        public static void NoFixWhenEndingWithNewLine()
        {
            var code = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""↓a\n"";
        }
    }
}";

            var title = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""\n"" +
                       ""a"";
        }
    }
}";
            RoslynAssert.NoRefactoring(Refactoring, code, title);
        }

        [Test]
        public static void StartingAndEndingWithNewLine()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""↓\na\n"";
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""\n"" +
                       ""a\n"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, before, after);
        }

        [Test]
        public static void OneCarriageReturnNewLine()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""↓a\r\nb"";
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""a\r\n"" +
                       ""b"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, before, after);
        }

        [Test]
        public static void OneCarriageReturnNewLineAndEndingWithCarriageReturnNewLine()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""↓a\r\nb\r\n"";
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""a\r\n"" +
                       ""b\r\n"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, before, after);
        }

        [Test]
        public static void TwoCarriageReturnNewLines()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""↓a\r\nb\r\nc"";
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""a\r\n"" +
                       ""b\r\n"" +
                       ""c"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, before, after);
        }

        [Test]
        public static void TwoCarriageReturnNewLinesAndEndingWithCarriageReturnNewLine()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""↓a\r\nb\r\nc\r\n"";
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""a\r\n"" +
                       ""b\r\n"" +
                       ""c\r\n"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, before, after);
        }

        [Test]
        public static void TwoNewLinesEndingWithNewline()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""↓a\nb\nc\n"";
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
            var text = ""a\n"" +
                       ""b\n"" +
                       ""c\n"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, before, after);
        }
    }
}
