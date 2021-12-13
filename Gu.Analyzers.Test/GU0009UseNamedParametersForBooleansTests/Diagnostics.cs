namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class CodeFix
    {
        private static readonly ArgumentAnalyzer Analyzer = new();
        private static readonly NameArgumentsFix Fix = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0009UseNamedParametersForBooleans);

        [Test]
        public static void UnnamedBooleanParameters()
        {
            var before = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class C
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
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class C
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

            RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic.WithMessage("Name the boolean argument"), before, after);
        }

        [Test]
        public static void HandlesAnAlias()
        {
            var before = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Alias = System.Boolean;

    public class C
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
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Alias = System.Boolean;

    public class C
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
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class C
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
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class C
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
