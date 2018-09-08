namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCachedTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly GU0051XmlSerializerNotCached Analyzer = new GU0051XmlSerializerNotCached();

        [Test]
        public void NoCreationsOfTheSerializer()
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
            
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CachedStaticReadonlyInitializedInlineXmlSerializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class Foo
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""));

        public Foo(int a, int b, int c, int d)
        {
            for(int i = 0; i < 100; ++i)
            {
            
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CachedStaticReadonlyInitializedInStaticConstructorXmlSerializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class Foo
    {
        private static readonly XmlSerializer serializer;

        static Foo()
        {
            serializer = new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""));
        }

        public Foo(int a, int b, int c, int d)
        {
            for(int i = 0; i < 100; ++i)
            {
            
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void CachedStaticInitializedInlineXmlSerializer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class Foo
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""));

        public Foo(int a, int b, int c, int d)
        {
            for(int i = 0; i < 100; ++i)
            {
            
            }
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [TestCase(@"new XmlSerializer(typeof(Foo), ""rootNode"")")]
        [TestCase(@"new XmlSerializer(typeof(Foo))")]
        public void NonLeakyConstructor(string code)
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
                XmlSerializer serializer = default(XmlSerializer);
            }
        }
    }
}";
            testCode = testCode.AssertReplace("default(XmlSerializer)", code);
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
