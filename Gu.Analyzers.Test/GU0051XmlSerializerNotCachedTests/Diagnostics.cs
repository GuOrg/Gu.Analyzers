namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCachedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Diagnostics
    {
        private static readonly GU0051XmlSerializerNotCached Analyzer = new GU0051XmlSerializerNotCached();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(GU0051XmlSerializerNotCached.Descriptor);

        [Test]
        public static void TrivialConstructionUnsaved()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            for(int i = 0; i < 100; ++i)
            {
                ↓new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""));
            }
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("The serializer is not cached."), code);
        }

        [Test]
        public static void TrivialConstructionUnsavedFullyQualified()
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            for(int i = 0; i < 100; ++i)
            {
                ↓new System.Xml.Serialization.XmlSerializer(typeof(Foo), new System.Xml.Serialization.XmlRootAttribute(""rootNode""));
            }
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [TestCase(@"new XmlSerializer(typeof(C), new XmlRootAttribute(""rootNode""))")]
        public static void LocalVariable(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class C
    {
        public C(int a, int b, int c, int d)
        {
            for(int i = 0; i < 100; ++i)
            {
                ↓XmlSerializer serializer = default(XmlSerializer);
            }
        }
    }
}".AssertReplace("default(XmlSerializer)", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [TestCase(@"new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""))")]
        public static void PrivateStaticVariableAssignedToMoreThanOnceInAForLoop(string code)
        {
            var testCode = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class Foo
    {
        private static XmlSerializer serializer;
    
        public Foo(int a, int b, int c, int d)
        {
            for(int i = 0; i < 100; ++i)
            {
                ↓serializer = default(XmlSerializer);
            }
        }
    }
}".AssertReplace("default(XmlSerializer)", code);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
