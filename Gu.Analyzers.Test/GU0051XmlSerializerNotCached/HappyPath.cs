namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCached
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<Analyzers.GU0051XmlSerializerNotCached>
    {
        [Test]
        public async Task CachedXmlSerializer()
        {
            var testCode = @"
using System;
using System.Xml.Serialization;

[Serializable()]
public class XMLObj
{
    [XmlElement(""block"")]
    public List<XMLnode> nodes{ get; set; }

    public XMLObj() { }
}

[Serializable]
public class Foo
{
    private static XmlSerializer serializer = new XmlSerializer(typeof(XMLObj), new XmlRootAttribute(""rootNode""));

    public Foo(int a, int b, int c, int d)
    {
        for(int i = 0; i < 100; ++i)
        {
            
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}