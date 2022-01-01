namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCachedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

internal static class Diagnostics
{
    private static readonly GU0051XmlSerializerNotCached Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.GU0051XmlSerializerNotCached);

    [Test]
    public static void TrivialConstructionUnsaved()
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
                ↓new XmlSerializer(typeof(C), new XmlRootAttribute(""rootNode""));
            }
        }
    }
}";

        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("The serializer is not cached"), code);
    }

    [Test]
    public static void TrivialConstructionUnsavedFullyQualified()
    {
        var code = @"
namespace N
{
    public class C
    {
        public C()
        {
            for(int i = 0; i < 100; ++i)
            {
                ↓new System.Xml.Serialization.XmlSerializer(typeof(C), new System.Xml.Serialization.XmlRootAttribute(""rootNode""));
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
    using System.Xml.Serialization;

    public class C
    {
        public C()
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

    [TestCase(@"new XmlSerializer(typeof(C), new XmlRootAttribute(""rootNode""))")]
    public static void PrivateStaticVariableAssignedToMoreThanOnceInAForLoop(string expression)
    {
        var code = @"
namespace N
{
    using System.Xml.Serialization;

    public class C
    {
        private static XmlSerializer? serializer;
    
        public C()
        {
            for(int i = 0; i < 100; ++i)
            {
                ↓serializer = default(XmlSerializer);
            }
        }
    }
}".AssertReplace("default(XmlSerializer)", expression);

        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code: code);
    }
}