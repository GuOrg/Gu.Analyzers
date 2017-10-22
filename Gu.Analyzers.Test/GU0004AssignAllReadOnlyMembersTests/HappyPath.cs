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
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingPropertiesStruct()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public struct Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; }

        public int B { get; }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticConstructorSettingProperties()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        static Foo()
        {
            A = 1;
            B = 2;
        }

        public static int A { get; }

        public static int B { get; }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingAllFields()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int b;

        public Foo(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ChainedConstructorSettingAllFields()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int b;
        
        public Foo()
            : this(1, 2)
        {
        }
     
        private Foo(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task WhenNoUninitializedFields()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a = 1;
        
        public Foo()
        {
        }
     
        public int A => a;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticConstructorSettingFields()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int A;

        public static readonly int B;

        static Foo()
        {
            A = 1;
            B = 2;
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task StaticConstructorSettingUninitializedField()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public static readonly int A;

        public static readonly int B = 2;

        static Foo()
        {
            A = 1;
        }
    }
}";

            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingReadonlyFieldIgnoringMutable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private bool disposed;

        public Foo(int a)
        {
            this.a = a;
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingPropertiesIgnoringMutable()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; private set; }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingReadonlyFieldIgnoringInitialized()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int a;
        private readonly int defaultValue = 5;

        public Foo(int a)
        {
            this.a = a;
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task ConstructorSettingPropertiesIgnoringInitialized()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int a)
        {
            this.A = a;
        }

        public int A { get; }

        public int B { get; } = 6;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreAbstract()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public abstract class Foo
    {
        protected Foo(int value)
        {
            this.Value = value;
        }

        public int Value { get; }

        public abstract int OtherValue { get; }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreIndexer()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public abstract class Foo
    {
        protected Foo(int value)
        {
            this.Value = value;
        }

        public int Value { get; }

        public int this[int index] => index;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreCalculatedStatementBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value;

        public Foo(int value)
        {
            this.value = value;
        }

        public int Value
        {
            get
            {
                return this.value;
            }
        }
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoreCalculatedExpressionBody()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public class Foo
    {
        private readonly int value;

        public Foo(int value)
        {
            this.value = value;
        }

        public int Value => this.value;
    }
}";
            await this.VerifyHappyPathAsync(testCode)
                      .ConfigureAwait(false);
        }
    }
}