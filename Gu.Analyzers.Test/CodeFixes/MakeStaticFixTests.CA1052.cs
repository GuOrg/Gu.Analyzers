namespace Gu.Analyzers.Test.CodeFixes
{
    using System.Collections.Immutable;

    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    internal static partial class MakeStaticFixTests
    {
        private static readonly FakeFxCopAnalyzer Analyzer = new();

        // ReSharper disable once InconsistentNaming
        private static readonly ExpectedDiagnostic CA1052 = ExpectedDiagnostic.Create(FakeFxCopAnalyzer.Descriptor);

        [TestCase("public")]
        [TestCase("internal")]
        public static void StaticHolderClass(string modifier)
        {
            var before = @"
namespace N
{
    public class ↓C
    {
        public static void M()
        {
        }
    }
}".AssertReplace("public", modifier);

            var after = @"
namespace N
{
    public static class C
    {
        public static void M()
        {
        }
    }
}".AssertReplace("public", modifier);
            RoslynAssert.CodeFix(Analyzer, Fix, CA1052, before, after);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class FakeFxCopAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor = new("CA1052", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
                Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
                context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ClassDeclaration);
            }

            private static void Handle(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is ClassDeclarationSyntax { Identifier: { } identifier })
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, identifier.GetLocation()));
                }
            }
        }
    }
}
