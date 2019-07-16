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

    internal partial class CodeFixWhenMissingParameterDocsSA1611
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FakeStyleCopAnalyzer();
        private static readonly CodeFixProvider Fix = new UselessDocsFix();

        [Test]
        public void ForFirstParameterWhenSummaryOnly()
        {
            var before = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        public StringBuilder Meh(StringBuilder ↓builder) => builder;
    }
}";

            var after = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh(StringBuilder builder) => builder;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [Test]
        public void ForFirstParameterWhenSummaryAndTypeParam()
        {
            var before = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <typeparam name=""T""></typeparam>
        public StringBuilder Meh<T>(StringBuilder ↓builder) => builder;
    }
}";

            var after = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <typeparam name=""T""></typeparam>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh<T>(StringBuilder builder) => builder;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [Test]
        public void ForFirstParameter()
        {
            var before = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""text"">The <see cref=""string""/>.</param>
        public StringBuilder Meh(StringBuilder ↓builder, string text) => builder;
    }
}";

            var after = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        /// <param name=""text"">The <see cref=""string""/>.</param>
        public StringBuilder Meh(StringBuilder builder, string text) => builder;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [Test]
        public void ForLastParameter()
        {
            var before = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""text"">The <see cref=""string""/>.</param>
        public StringBuilder Meh(string text, StringBuilder ↓builder) => builder;
    }
}";

            var after = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""text"">The <see cref=""string""/>.</param>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh(string text, StringBuilder builder) => builder;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [Test]
        public void ForMiddleParameter()
        {
            var before = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        /// <param name=""builder2"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh(StringBuilder builder, StringBuilder ↓text, StringBuilder builder2) => builder;
    }
}";

            var after = @"
namespace N
{
    using System.Text;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        /// <param name=""text"">The <see cref=""StringBuilder""/>.</param>
        /// <param name=""builder2"">The <see cref=""StringBuilder""/>.</param>
        public StringBuilder Meh(StringBuilder builder, StringBuilder text, StringBuilder builder2) => builder;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [Test]
        public void ForGenericParameter()
        {
            var before = @"
namespace N
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Controls;

    /// <summary>
    /// A collection of <see cref=""System.Windows.Controls.ColumnDefinition""/>
    /// </summary>
    public class ColumnDefinitions : Collection<System.Windows.Controls.ColumnDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref=""ColumnDefinitions""/> class.
        /// </summary>
        public ColumnDefinitions(IList<ColumnDefinition> ↓collection)
            : base(collection)
        {
        }
    }
}";

            var after = @"
namespace N
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Controls;

    /// <summary>
    /// A collection of <see cref=""System.Windows.Controls.ColumnDefinition""/>
    /// </summary>
    public class ColumnDefinitions : Collection<System.Windows.Controls.ColumnDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref=""ColumnDefinitions""/> class.
        /// </summary>
        /// <param name=""collection"">The <see cref=""IList{ColumnDefinition}""/>.</param>
        public ColumnDefinitions(IList<ColumnDefinition> collection)
            : base(collection)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [TestCase("int")]
        [TestCase("string")]
        public void EmptyForPrimitiveTypes(string type)
        {
            var before = @"
namespace N
{
    public class C
    {
        /// <summary>
        /// Summary
        /// </summary>
        public static void M(string ↓text)
        {
        }
    }
}".AssertReplace("string", type);

            var after = @"
namespace N
{
    public class C
    {
        /// <summary>
        /// Summary
        /// </summary>
        /// <param name=""text""></param>
        public static void M(string text)
        {
        }
    }
}".AssertReplace("string", type);
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class FakeStyleCopAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor("SA1611", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
                Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
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
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, parameter.Identifier.GetLocation()));
                }
            }
        }
    }
}
