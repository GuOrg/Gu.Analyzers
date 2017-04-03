namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCached
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class Diagnostics : DiagnosticVerifier<Analyzers.GU0051XmlSerializerNotCached>
    {
        [TestCase(@"new XmlSerializer(typeof(XMLObj), new XmlRootAttribute(""rootNode""))")]
        public async Task LanguageConstructs(string code)
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

public class Foo
{
    public Foo(int a, int b, int c, int d)
    {
        for(int i = 0; i < 100; ++i)
        {
            ↓XmlSerializer serializer = default(XmlSerializer);
        }
    }
}";
            testCode = testCode.AssertReplace("default(XmlSerializer)", code);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("Cache the XmlSerializer.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }
    }
}