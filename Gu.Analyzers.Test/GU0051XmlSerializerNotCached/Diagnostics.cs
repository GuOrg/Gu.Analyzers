namespace Gu.Analyzers.Test.GU0051XmlSerializerNotCached
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal partial class Diagnostics : DiagnosticVerifier<Analyzers.GU0051XmlSerializerNotCached>
    {
        [Test]
        public async Task TrivialConstructionUnsaved()
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
            ↓new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""));
        }
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The serializer is not cached.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [TestCase(@"new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""))")]
        public async Task LocalVariable(string code)
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
            ↓XmlSerializer serializer = default(XmlSerializer);
        }
    }
}";
            testCode = testCode.AssertReplace("default(XmlSerializer)", code);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The serializer is not cached.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [TestCase(@"new XmlSerializer(typeof(Foo), new XmlRootAttribute(""rootNode""))")]
        public async Task PrivateStaticVariableAssignedToMoreThanOnceInAForLoop(string code)
        {
            var testCode = @"
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
}";
            testCode = testCode.AssertReplace("default(XmlSerializer)", code);
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The serializer is not cached.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }
    }
}