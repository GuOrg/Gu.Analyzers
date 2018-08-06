namespace Gu.Analyzers.Test.GenerateUselessDocsFixTests
{
    using System.Collections.Immutable;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal partial class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new DummyAnalyzer();
        private static readonly CodeFixProvider Fix = new GenerateUselessDocsFixProvider();

        [Test]
        public void ForFirstParameterWhenSummaryOnly()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        public StringBuilder Meh(↓StringBuilder builder) => builder;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh(StringBuilder builder) => builder;
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [Test]
        public void ForFirstParameterWhenSummaryAndTypeParam()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <typeparam name=""T""></typeparam>
        public StringBuilder Meh<T>(↓StringBuilder builder) => builder;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <typeparam name=""T""></typeparam>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh<T>(StringBuilder builder) => builder;
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [Test]
        public void ForFirstParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""text"">The <see cref=""string""/>.</param>
        public StringBuilder Meh(↓StringBuilder builder, string text) => builder;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        /// <param name=""text"">The <see cref=""string""/>.</param>
        public StringBuilder Meh(StringBuilder builder, string text) => builder;
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [Test]
        public void ForLastParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh(StringBuilder builder, ↓string text) => builder;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        /// <param name=""text"">The <see cref=""string""/>.</param>
        public StringBuilder Meh(StringBuilder builder, string text) => builder;
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [Test]
        public void ForMiddleParameter()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        /// <param name=""builder2"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh(StringBuilder builder, ↓string text, StringBuilder builder2) => builder;
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Text;

    public class Foo
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        /// <param name=""text"">The <see cref=""string""/>.</param>
        /// <param name=""builder2"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh(StringBuilder builder, string text, StringBuilder builder2) => builder;
    }
}";
            AnalyzerAssert.CodeFix(Analyzer, Fix, testCode, fixedCode);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class DummyAnalyzer : DiagnosticAnalyzer
        {
            public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor("SA1611", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
                Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(this.Handle, SyntaxKind.Parameter);
            }

            private void Handle(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is ParameterSyntax parameter &&
                    parameter.Parent is ParameterListSyntax parameterList &&
                    parameterList.Parent is BaseMethodDeclarationSyntax methodDeclaration &&
                    methodDeclaration.HasLeadingTrivia &&
                    !methodDeclaration.GetLeadingTrivia().ToString().Contains(parameter.Identifier.ValueText))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
        }
    }
}
