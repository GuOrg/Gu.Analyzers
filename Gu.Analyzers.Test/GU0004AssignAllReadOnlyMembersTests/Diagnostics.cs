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