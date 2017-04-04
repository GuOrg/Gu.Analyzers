namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCached
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<Analyzers.GU0051XmlSerializerNotCached>
    {
        [Test]
        public async Task NoCreationsOfTheSerializer()
        {
            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task CachedXmlSerializer()
        {
            var testCode = @"
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

public class Foo
{
    private static XmlSerializer serializer = new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""));

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