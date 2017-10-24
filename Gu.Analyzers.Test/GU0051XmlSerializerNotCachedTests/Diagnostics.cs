namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCachedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        [Test]
        public void TrivialConstructionUnsaved()
        {
            var testCode = @"
namespace RoslynSandbox
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
            var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated(
                diagnosticId: "GU0051",
                message: "The serializer is not cached.",
                code: testCode,
                cleanedSources: out testCode);
            AnalyzerAssert.Diagnostics<GU0051XmlSerializerNotCached>(expectedDiagnostic, testCode);
        }

        [Test]
        public void TrivialConstructionUnsavedFullyQualified()
        {
            var testCode = @"
namespace RoslynSandbox
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
            AnalyzerAssert.Diagnostics<GU0051XmlSerializerNotCached>(testCode);
        }

        [TestCase(@"new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""))")]
        public void LocalVariable(string code)
        {
            var testCode = @"
namespace RoslynSandbox
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
                ↓XmlSerializer serializer = default(XmlSerializer);
            }
        }
    }
}";
            testCode = testCode.AssertReplace("default(XmlSerializer)", code);
            AnalyzerAssert.Diagnostics<GU0051XmlSerializerNotCached>(testCode);
        }

        [TestCase(@"new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""))")]
        public void PrivateStaticVariableAssignedToMoreThanOnceInAForLoop(string code)
        {
            var testCode = @"
namespace RoslynSandbox
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
}";
            testCode = testCode.AssertReplace("default(XmlSerializer)", code);
            AnalyzerAssert.Diagnostics<GU0051XmlSerializerNotCached>(testCode);
        }
    }
}