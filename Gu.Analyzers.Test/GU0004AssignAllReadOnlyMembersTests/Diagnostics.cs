namespace Gu.Analyzers.Test.GU0004AssignAllReadOnlyMembersTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class Diagnostics : DiagnosticVerifier<GU0004AssignAllReadOnlyMembers>
    {
        [Test]
        public async Task NotSettingGetOnlyProperty()
        {
            var testCode = @"
    public class Foo
    {
        ↓public Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The following readonly members are not assigned: B.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task NotSettingGetOnlyPropertyInOneCtor()
        {
            var testCode = @"
namespace RoslyynSandbox
{
    using System;
    using System.ComponentModel;
    using System.Windows.Markup;

    public class DisplayPropertyNameExtension : MarkupExtension
    {
        ↓public DisplayPropertyNameExtension()
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

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The following readonly members are not assigned: PropertyName.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task NotSettingReadOnlyField()
        {
            var testCode = @"
    public class Foo
    {
        private readonly int a;
        private readonly int b;

        ↓public Foo(int a)
        {
            this.a = a;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The following readonly members are not assigned: b.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task StaticConstructorSettingProperties()
        {
            var testCode = @"
    public class Foo
    {
        ↓static Foo()
        {
            A = 1;
        }

        public static int A { get; }

        public static int B { get; }
    }";
            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The following readonly members are not assigned: B.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }

        [Test]
        public async Task StaticConstructorNotSettingField()
        {
            var testCode = @"
    public class Foo
    {
        public static readonly int A;

        public static readonly int B;

        ↓static Foo()
        {
            A = 1;
        }
    }";

            var expected = this.CSharpDiagnostic()
                               .WithLocationIndicated(ref testCode)
                               .WithMessage("The following readonly members are not assigned: B.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);
        }
    }
}