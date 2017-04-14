namespace Gu.Analyzers.Test.GU0009UseNamedParametersForBooleansTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0009UseNamedParametersForBooleans>
    {
        [Test]
        public async Task UnnamedBooleanParameters()
        {
            var testCode = @"
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

public class Foo
{
    public void Floof(int howMuch, bool useFluffyBuns)
    {
        
    }

    public void Another()
    {
        Floof(42, ↓false);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The boolean parameter is not named.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task HandlesAnAlias()
        {
            var testCode = @"
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Alias = System.Boolean;

public class Foo
{
    public void Floof(int howMuch, Alias useFluffyBuns)
    {
        
    }

    public void Another()
    {
        Floof(42, ↓false);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The boolean parameter is not named.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task HandlesAFullyQualifiedName()
        {
            var testCode = @"
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

public class Foo
{
    public void Floof(int howMuch, System.Boolean useFluffyBuns)
    {
        
    }

    public void Another()
    {
        Floof(42, ↓false);
    }
}";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The boolean parameter is not named.");
            await this.VerifyCSharpDiagnosticAsync(new[] { testCode }, expected)
                      .ConfigureAwait(false);
        }
    }
}