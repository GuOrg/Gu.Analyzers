namespace Gu.Analyzers.Test.GU0052ExceptionShouldBeSerializableTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class HappyPath
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GU0052ExceptionShouldBeSerializable();

        [Test]
        public void WhenNoBaseClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
public class Foo
{
}
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void WhenSerializable()
        {
            var testCode = @"
namespace RoslynSandbox
{
using System;

[Serializable]
public class FooException : Exception
{
    public FooException()
        : base(string.Empty)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public void ExtendedWithAttribute()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    
    [Serializable]
    public class FooException : Exception
    {
        public FooException()
        : base(string.Empty)
        {
        }
    }

    [Serializable]
    public class BarException : FooException
    {
        public BarException()
        : base(string.Empty)
        {
        }
    }
}";
            AnalyzerAssert.Valid(Analyzer, testCode);
        }
    }
}
