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

    internal static class CodeFixWhenEmptyParameterDocsSA1614
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FakeStyleCopAnalyzer();
        private static readonly CodeFixProvider Fix = new DocsFix();

        [Test]
        public static void StandardTextForCancellationTokenWhenEmpty()
        {
            var before = @"
namespace N
{
    using System.Threading;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// ↓<param name=""cancellationToken""></param>
        public void Meh(CancellationToken cancellationToken)
        {
        }
    }
}";

            var after = @"
namespace N
{
    using System.Threading;

    public class C
    {
        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name=""cancellationToken"">The <see cref=""CancellationToken""/> that cancels the operation.</param>
        public void Meh(CancellationToken cancellationToken)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after, fixTitle: "Generate standard xml documentation for parameter.");
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class FakeStyleCopAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor Descriptor = new("SA1614", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
                Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
                context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.XmlElement);
            }

            private static void Handle(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is XmlElementSyntax element &&
                    !element.Content.Any())
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
        }
    }
}
