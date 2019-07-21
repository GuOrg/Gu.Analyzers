namespace Gu.Analyzers.Test.GU0090DoNotThrowNotImplementedExceptionTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ExceptionAnalyzer();

        [Test]
        public static void Rethrowing()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class Exceptions
    {
        private readonly string path;

        public Exceptions(string text)
        {
            this.path = text ?? throw new System.ArgumentNullException(nameof(text));
        }

        public void M()
        {
            try
            {
                _ = File.ReadAllText(this.path);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e);
                throw;
            }

            try
            {
                _ = File.ReadAllText(this.path);
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            try
            {
                _ = File.ReadAllText(this.path);
            }
            catch
            {
                throw;
            }
        }
    }
}
";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
