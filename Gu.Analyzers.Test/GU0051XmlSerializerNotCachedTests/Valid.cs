namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCachedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Valid
{
    private static readonly GU0051XmlSerializerNotCached Analyzer = new();

    [Test]
    public static void NoCreationsOfTheSerializer()
    {
        var code = @"
namespace N
{
    public class C
    {
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void CachedStaticReadonlyInitializedInlineXmlSerializer()
    {
        var code = @"
namespace N
{
    using System.Xml.Serialization;

    public class C
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(C), new XmlRootAttribute(""rootNode""));
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void CachedStaticReadonlyInitializedInStaticConstructorXmlSerializer()
    {
        var code = @"
namespace N
{
    using System.Xml.Serialization;

    public class C
    {
        private static readonly XmlSerializer serializer;

        static C()
        {
            serializer = new XmlSerializer(typeof(C), new XmlRootAttribute(""rootNode""));
        }

        public C()
        {
            for(int i = 0; i < 100; ++i)
            {
            
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void CachedStaticInitializedInlineXmlSerializer()
    {
        var code = @"
namespace N
{
    using System.Xml.Serialization;

    public class C
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(C), new XmlRootAttribute(""rootNode""));

        public C()
        {
            for(int i = 0; i < 100; ++i)
            {
            
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase(@"new XmlSerializer(typeof(C), ""rootNode"")")]
    [TestCase(@"new XmlSerializer(typeof(C))")]
    public static void NonLeakyConstructor(string expression)
    {
        var code = @"
namespace N
{
    using System.Xml.Serialization;

    public class C
    {
        public C()
        {
            for(int i = 0; i < 100; ++i)
            {
                XmlSerializer serializer = default(XmlSerializer);
            }
        }
    }
}".AssertReplace("default(XmlSerializer)", expression);

        RoslynAssert.Valid(Analyzer, code);
    }
}