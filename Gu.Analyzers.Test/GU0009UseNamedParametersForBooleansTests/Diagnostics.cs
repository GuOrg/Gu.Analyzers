namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0009UseNamedParametersForBooleans();
        private static readonly CodeFixProvider Fix = new NameArgumentsFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0009UseNamedParametersForBooleans.DiagnosticId);

        [Test]
        public static void UnnamedBooleanParameters()
        {
            var before = @"
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

            var after = @"
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
            Floof(42, useFluffyBuns: false);
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic.WithMessage("The boolean parameter is not named."), before, after);
        }

        [Test]
        public static void HandlesAnAlias()
        {
            var before = @"
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

            var after = @"
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
            Floof(42, useFluffyBuns: false);
        }
    }
}";

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }

        [Test]
        public static void HandlesAFullyQualifiedName()
        {
            var before = @"
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

            var after = @"
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
            Floof(42, useFluffyBuns: false);
        }
    }
}";
            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
        }
    }
}
