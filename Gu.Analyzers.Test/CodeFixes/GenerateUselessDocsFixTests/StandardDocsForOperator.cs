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

    internal static class StandardDocsForOperator
    {
        private static readonly DiagnosticAnalyzer Analyzer = new FakeAnalyzer();
        private static readonly CodeFixProvider Fix = new DocsFix();

        [Test]
        public static void OperatorEquals()
        {
            var before = @"
namespace N
{
    public sealed class C
    {
        public static bool operator ↓==(C left, C right)
        {
            return Equals(left, right);
        }

        /// <summary>Check if <paramref name=""left""/> is not equal to <paramref name=""right""/>.</summary>
        /// <param name=""left"">The left <see cref=""C""/>.</param>
        /// <param name=""right"">The right <see cref=""C""/>.</param>
        /// <returns>True if <paramref name=""left""/> is not equal to <paramref name=""right""/>.</returns>
        public static bool operator !=(C left, C right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is C;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 1;
    }
}";

            var after = @"
namespace N
{
    public sealed class C
    {
        /// <summary>Check if <paramref name=""left""/> is equal to <paramref name=""right""/>.</summary>
        /// <param name=""left"">The left <see cref=""C""/>.</param>
        /// <param name=""right"">The right <see cref=""C""/>.</param>
        /// <returns>True if <paramref name=""left""/> is equal to <paramref name=""right""/>.</returns>
        public static bool operator ==(C left, C right)
        {
            return Equals(left, right);
        }

        /// <summary>Check if <paramref name=""left""/> is not equal to <paramref name=""right""/>.</summary>
        /// <param name=""left"">The left <see cref=""C""/>.</param>
        /// <param name=""right"">The right <see cref=""C""/>.</param>
        /// <returns>True if <paramref name=""left""/> is not equal to <paramref name=""right""/>.</returns>
        public static bool operator !=(C left, C right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is C;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 1;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [Test]
        public static void OperatorNotEquals()
        {
            var before = @"
namespace N
{
    public sealed class C
    {
        /// <summary>Check if <paramref name=""left""/> is equal to <paramref name=""right""/>.</summary>
        /// <param name=""left"">The left <see cref=""C""/>.</param>
        /// <param name=""right"">The right <see cref=""C""/>.</param>
        /// <returns>True if <paramref name=""left""/> is equal to <paramref name=""right""/>.</returns>
        public static bool operator ==(C left, C right)
        {
            return Equals(left, right);
        }

        public static bool operator ↓!=(C left, C right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is C;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 1;
    }
}";

            var after = @"
namespace N
{
    public sealed class C
    {
        /// <summary>Check if <paramref name=""left""/> is equal to <paramref name=""right""/>.</summary>
        /// <param name=""left"">The left <see cref=""C""/>.</param>
        /// <param name=""right"">The right <see cref=""C""/>.</param>
        /// <returns>True if <paramref name=""left""/> is equal to <paramref name=""right""/>.</returns>
        public static bool operator ==(C left, C right)
        {
            return Equals(left, right);
        }

        /// <summary>Check if <paramref name=""left""/> is not equal to <paramref name=""right""/>.</summary>
        /// <param name=""left"">The left <see cref=""C""/>.</param>
        /// <param name=""right"">The right <see cref=""C""/>.</param>
        /// <returns>True if <paramref name=""left""/> is not equal to <paramref name=""right""/>.</returns>
        public static bool operator !=(C left, C right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is C;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 1;
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, before, after);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class FakeAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor Descriptor = new("CS1591", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
                Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
                context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.OperatorDeclaration);
            }

            private static void Handle(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is OperatorDeclarationSyntax declaration &&
                    !declaration.HasStructuredTrivia)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, declaration.OperatorToken.GetLocation()));
                }
            }
        }
    }
}
