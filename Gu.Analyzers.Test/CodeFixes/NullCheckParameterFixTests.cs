namespace Gu.Analyzers.Test.CodeFixes
{
    using System.Collections.Immutable;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class NullCheckParameterFixTests
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FakeFxCopAnalyzer();
        private static readonly CodeFixProvider Fix = new NullCheckParameterFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(FakeFxCopAnalyzer.Descriptor);

        [Test]
        public static void Simple()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M(string s)
        {
            _ = ↓s.Length;
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M(string s)
        {
            if (s is null)
            {
                throw new System.ArgumentNullException(nameof(s));
            }

            _ = s.Length;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ExpressionBody()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static int M(string s) => ↓s.Length;
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static int M(string s)
        {
            if (s is null)
            {
                throw new System.ArgumentNullException(nameof(s));
            }

            return s.Length;
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void ExpressionBodyVoid()
        {
            var before = @"
namespace N
{
    public static class C
    {
        public static void M1(string s) => M2(↓s.Length);

        private static void M2(int n)
        {
        }
    }
}";

            var after = @"
namespace N
{
    public static class C
    {
        public static void M1(string s)
        {
            if (s is null)
            {
                throw new System.ArgumentNullException(nameof(s));
            }

            M2(s.Length);
        }

        private static void M2(int n)
        {
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class FakeFxCopAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor = new("CA1062", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
                Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
                context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.IdentifierName);
            }

            private static void Handle(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is IdentifierNameSyntax element &&
                    element.Parent is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression == element)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, element.GetLocation()));
                }
            }
        }
    }
}
