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
            var code = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""↓\na"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""\n"" +
                       ""a"";
        }
    }
}";
            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public static void NoFixWhenEndingWithNewLine()
        {
            var code = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""↓a\n"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""\n"" +
                       ""a"";
        }
    }
}";
            RoslynAssert.NoRefactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public static void StartingAndEndingWithNewLine()
        {
            var code = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""↓\na\n"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""\n"" +
                       ""a\n"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public static void OneCarriageReturnNewLine()
        {
            var code = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""↓a\r\nb"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""a\r\n"" +
                       ""b"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public static void OneCarriageReturnNewLineAndEndingWithCarriageReturnNewLine()
        {
            var code = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""↓a\r\nb\r\n"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""a\r\n"" +
                       ""b\r\n"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public static void TwoCarriageReturnNewLines()
        {
            var code = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""↓a\r\nb\r\nc"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""a\r\n"" +
                       ""b\r\n"" +
                       ""c"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public static void TwoCarriageReturnNewLinesAndEndingWithCarriageReturnNewLine()
        {
            var code = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""↓a\r\nb\r\nc\r\n"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""a\r\n"" +
                       ""b\r\n"" +
                       ""c\r\n"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }

        [Test]
        public static void TwoNewLinesEndingWithNewline()
        {
            var code = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""↓a\nb\nc\n"";
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public static class C
    {
        public static void Test()
        {
            var text = ""a\n"" +
                       ""b\n"" +
                       ""c\n"";
        }
    }
}";

            RoslynAssert.Refactoring(Refactoring, code, fixedCode);
        }
    }
}
