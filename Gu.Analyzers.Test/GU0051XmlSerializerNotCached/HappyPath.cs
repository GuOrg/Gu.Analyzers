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
        public async Task CachedStaticReadonlyInitializedInlineXmlSerializer()
        {
            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task CachedStaticReadonlyInitializedInStaticConstructorXmlSerializer()
        {
            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task CachedStaticInitializedInlineXmlSerializer()
        {
            var testCode = @"
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
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [TestCase(@"new XmlSerializer(typeof(Foo), ""rootNode"")")]
        [TestCase(@"new XmlSerializer(typeof(Foo))")]
        public async Task NonLeakyConstructor(string code)
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
            XmlSerializer serializer = default(XmlSerializer);
        }
    }
}";
            testCode = testCode.AssertReplace("default(XmlSerializer)", code);
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}