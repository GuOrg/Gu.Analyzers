namespace Gu.Analyzers.Test.GU0004AssignAllReadOnlyMembersTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class Diagnostics
    {
        private static readonly GU0004AssignAllReadOnlyMembers Analyzer = new GU0004AssignAllReadOnlyMembers();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("GU0004");

        [Test]
        public void Message()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public ↓Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; }
    }
}";

            var message = "The following readonly members are not assigned:\r\n" +
                          "RoslynSandbox.Foo.B";
            var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated("GU0004", message, testCode, out testCode);
            AnalyzerAssert.Diagnostics(Analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void NotSettingGetOnlyProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public ↓Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, testCode);
        }

        [Test]
        public void NotSettingGetOnlyPropertyInOneCtor()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.ComponentModel;
    using System.Windows.Markup;

    public class DisplayPropertyNameExtension : MarkupExtension
    {
        public ↓DisplayPropertyNameExtension()
        {
        }

        public DisplayPropertyNameExtension(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        public Type Type { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // (This code has zero tolerance)
            var prop = Type.GetProperty(PropertyName);
            var attributes = prop.GetCustomAttributes(typeof(DisplayNameAttribute), inherit: false);
            return (attributes[0] as DisplayNameAttribute)?.DisplayName;
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void NotSettingReadOnlyField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int b;

        public ↓Foo(int a)
        {
            this.a = a;
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void StaticConstructorSettingProperties()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        static ↓Foo()
        {
            A = 1;
        }

        public static int A { get; }

        public static int B { get; }
    }
}";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public void StaticConstructorNotSettingField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int A;

        public static readonly int B;

        static ↓Foo()
        {
            A = 1;
        }
    }
}";

            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}