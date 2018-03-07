namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly GU0009UseNamedParametersForBooleans Analyzer = new GU0009UseNamedParametersForBooleans();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0009");

        [Test]
        public void UnnamedBooleanParameters()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class Foo
    {
        public void Floof(int howMuch, bool useFluffyBuns)
        {
        
        }

        public void Another()
        {
            Floof(42, ↓false);
        }
    }
}";
            var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated(
                diagnosticId: "GU0009",
                message: "The boolean parameter is not named.",
                code: testCode,
                cleanedSources: out testCode);
            AnalyzerAssert.Diagnostics(Analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void HandlesAnAlias()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Alias = System.Boolean;

    public class Foo
    {
        public void Floof(int howMuch, Alias useFluffyBuns)
        {
        
        }

        public void Another()
        {
            Floof(42, ↓false);
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void HandlesAFullyQualifiedName()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class Foo
    {
        public void Floof(int howMuch, System.Boolean useFluffyBuns)
        {
        
        }

        public void Another()
        {
            Floof(42, ↓false);
        }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}