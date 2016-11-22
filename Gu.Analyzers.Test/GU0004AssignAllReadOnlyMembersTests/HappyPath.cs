namespace Gu.Analyzers.Test.GU0004AssignAllReadOnlyMembersTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class HappyPath : HappyPathVerifier<GU0004AssignAllReadOnlyMembers>
    {
        [Test]
        public async Task ConstructorSettingProperties()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticConstructorSettingProperties()
        {
            var testCode = @"
    public class Foo
    {
        static Foo()
        {
            A = 1;
            B = 2;
        }

        public static int A { get; }

        public static int B { get; }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingAllFields()
        {
            var testCode = @"
    public class Foo
    {
        private readonly int a;
        private readonly int b;

        public Foo(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticConstructorSettingFields()
        {
            var testCode = @"
    public class Foo
    {
        public static readonly int A;

        public static readonly int B;

        static Foo()
        {
            A = 1;
            B = 2;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingReadonlyFieldIgnoringMutable()
        {
            var testCode = @"
    public class Foo
    {
        private readonly int a;
        private bool disposed;

        public Foo(int a)
        {
            this.a = a;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingPropertiesIgnoringMutable()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; private set; }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingReadonlyFieldIgnoringInitialized()
        {
            var testCode = @"
    public class Foo
    {
        private readonly int a;
        private readonly int defaultValue = 5;

        public Foo(int a)
        {
            this.a = a;
        }
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingPropertiesIgnoringInitialized()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; } = 6;
    }";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}