namespace Gu.Analyzers.Test.CodeFixes.GenerateUselessDocsFixTests
{
    using System.Collections.Immutable;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class CodeFixWhenMissingTypeParameterDocsSA1618
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FakeStyleCopAnalyzer();
        private static readonly CodeFixProvider Fix = new UselessDocsFix();

        [Test]
        public void ForFirstParameterWhenSummaryOnly()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        public static void M<↓T>(T item)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <typeparam name=""T"">The type of <paramref name=""item""/>.</typeparam>
        public static void M<T>(T item)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [Test]
        public void ForFirstParameterWhenSummaryAndParam()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""item"">text.</param>
        public static void M<↓T>(T item)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <typeparam name=""T"">The type of <paramref name=""item""/>.</typeparam>
        /// <param name=""item"">text.</param>
        public static void M<T>(T item)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [Test]
        public void ForManyParameters()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        public static void M<↓T>(T x, T y)
        {
        }
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <typeparam name=""T""></typeparam>
        public static void M<T>(T x, T y)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [Test]
        public void Issue206()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        /// <summary>
        /// Saves <paramref name=""item""/> as json.
        /// </summary>
        /// <param name=""fileName"">The file name.</param>
        public static void Save<↓T>(string fileName, T item)
        {
        }
        }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    public class C
    {
        /// <summary>
        /// Saves <paramref name=""item""/> as json.
        /// </summary>
        /// <typeparam name=""T"">The type of <paramref name=""item""/>.</typeparam>
        /// <param name=""fileName"">The file name.</param>
        public static void Save<T>(string fileName, T item)
        {
        }
        }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [TestCase("T[]")]
        [TestCase("List<T>")]
        [TestCase("System.Collections.Generic.List<T>")]
        public void ForEnumerableParameter(string type)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""xs"">text.</param>
        public static void M<↓T>(T[] xs)
        {
        }
    }
}".AssertReplace("T[]", type);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Collections.Generic;

    public class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <typeparam name=""T"">The type of the elements in <paramref name=""xs""/>.</typeparam>
        /// <param name=""xs"">text.</param>
        public static void M<T>(T[] xs)
        {
        }
    }
}".AssertReplace("T[]", type);
            RoslynAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class FakeStyleCopAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                "SA1618",
                "Title",
                "Message",
                "Category",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
                Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
                context.RegisterSyntaxNodeAction(this.Handle, SyntaxKind.TypeParameter);
            }

            private void Handle(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is TypeParameterSyntax parameter)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, parameter.Identifier.GetLocation()));
                }
            }
        }
    }
}
