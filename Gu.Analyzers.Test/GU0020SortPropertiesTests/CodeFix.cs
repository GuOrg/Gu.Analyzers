namespace Gu.Analyzers.Test.GU0020SortPropertiesTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class CodeFix : CodeFixVerifier<GU0020SortProperties, SortPropertiesCodeFixProvider>
    {
        [Test]
        public async Task ExplicitImplementation()
        {
            var interfaceCode = @"    
    interface IValue
    {
        object Value { get; }
    }";

            var testCode = @"
    public class Foo : IValue
    {
        private int Value { get; } = 5;

        object IValue.Value { get; } = 5;
    }";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 4, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 6, 9)
                                .WithMessage("Move property.");

            await this.VerifyCSharpDiagnosticAsync(new[] { interfaceCode, testCode }, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    public class Foo : IValue
    {
        object IValue.Value { get; } = 5;

        private int Value { get; } = 5;
    }";
            await this.VerifyCSharpFixAsync(new[] { interfaceCode, testCode }, new[] { interfaceCode, fixedCode }, codeFixIndex: 0).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenMutableBeforeGetOnlyFirst()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; set; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 12, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 14, 9)
                                .WithMessage("Move property.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public int A { get; set; }
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, codeFixIndex: 0).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenMutableBeforeGetOnlyFirstWithNamespaces()
        {
            var testCode = @"
namespace Test
{
    using System;

    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; set; }

        public int B { get; }

        public int C { get; }

        public int D { get; }
    }
}";

            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 16, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 18, 9)
                                .WithMessage("Move property.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
namespace Test
{
    using System;

    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public int A { get; set; }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, codeFixIndex: 0).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenMutableBeforeGetOnlyLast()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int C { get; set; }

        public int D { get; }
    }";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 16, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 18, 9)
                                .WithMessage("Move property.");
            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        public int D { get; }

        public int C { get; set; }
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenPrivateSetAfterPublicSet()
        {
            var testCode = @"
public class Foo
{
    private int c;
    private int d;

    public int A { get; }

    public int B
    {
        get
        {
            return this.A;
        }
    }

    public int C
    {
        get
        {
            return this.c;
        }
        set
        {
            this.c = value;
        }
    }

    public int D
    {
        get
        {
            return this.d;
        }
        private set
        {
            this.d = value;
        }
    }
}";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 17, 5)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 29, 5)
                                .WithMessage("Move property.");
            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
public class Foo
{
    private int c;
    private int d;

    public int A { get; }

    public int B
    {
        get
        {
            return this.A;
        }
    }

    public int D
    {
        get
        {
            return this.d;
        }
        private set
        {
            this.d = value;
        }
    }

    public int C
    {
        get
        {
            return this.c;
        }
        set
        {
            this.c = value;
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenMutableBeforeGetOnlyFirstWithInitializers()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; set; } = 1;

        public int B { get; } = 2;

        public int C { get; } = 3;

        public int D { get; } = 4;
    }";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 12, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 14, 9)
                                .WithMessage("Move property.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int B { get; } = 2;

        public int C { get; } = 3;

        public int D { get; } = 4;

        public int A { get; set; } = 1;
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task WhenMutableBeforeGetOnlyWithComments()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        /// <summary>
        /// C
        /// </summary>
        public int C { get; set; }

        /// <summary>
        /// D
        /// </summary>
        public int D { get; }
    }";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 19, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 24, 9)
                                .WithMessage("Move property.");
            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    public class Foo
    {
        public Foo(int a, int b, int c, int d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public int A { get; }

        public int B { get; }

        /// <summary>
        /// D
        /// </summary>
        public int D { get; }

        /// <summary>
        /// C
        /// </summary>
        public int C { get; set; }
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task ExpressionBodyBeforeGetOnly()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int b)
        {
            this.B = b;
        }

        public int A => B;

        public int B { get; }
    }";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 9, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 11, 9)
                                .WithMessage("Move property.");
            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    public class Foo
    {
        public Foo(int b)
        {
            this.B = b;
        }

        public int B { get; }

        public int A => B;
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task CalculatedBeforeGetOnly()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int b)
        {
            this.B = b;
        }

        public int A
        {
            get
            {
                return B;
            }
        }

        public int B { get; }
    }";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 9, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 17, 9)
                                .WithMessage("Move property.");
            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    public class Foo
    {
        public Foo(int b)
        {
            this.B = b;
        }

        public int B { get; }

        public int A
        {
            get
            {
                return B;
            }
        }
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task IndexerBeforeMutable()
        {
            var testCode = @"
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Foo : IReadOnlyList<int>
    {
        public int Count { get; }

        public int this[int index]
        {
            get
            {
                if (index != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, ""message"");
                }

                return A;
            }
        }

        public int A { get; set; }

        public IEnumerator<int> GetEnumerator()
        {
            yield return A;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }";
            var expected1 = this.CSharpDiagnostic()
                               .WithLocation("Foo.cs", 10, 9)
                               .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 23, 9)
                                .WithMessage("Move property.");
            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Foo : IReadOnlyList<int>
    {
        public int Count { get; }

        public int A { get; set; }

        public int this[int index]
        {
            get
            {
                if (index != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, ""message"");
                }

                return A;
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            yield return A;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task PublicSetBeforePrivateSetFirst()
        {
            var testCode = @"
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int A { get; set; }

        public int B { get; private set; }
    }";
            var expected1 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 10, 9)
                                .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 12, 9)
                                .WithMessage("Move property.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
    public class Foo
    {
        public Foo(int a, int b)
        {
            this.A = a;
            this.B = b;
        }

        public int B { get; private set; }

        public int A { get; set; }
    }";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Test]
        public async Task NestedClass()
        {
            var testCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo()
        {
        }

        public int Bar
        {
            get { return this.Bar; }
            set { this.Bar = value; }
        }

        public class Nested
        {
            public int Value1 { get; set; }
            
            public int Value2 { get; private set; }
        }
    }
}";
            var expected1 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 18, 13)
                                .WithMessage("Move property.");
            var expected2 = this.CSharpDiagnostic()
                                .WithLocation("Foo.cs", 20, 13)
                                .WithMessage("Move property.");

            await this.VerifyCSharpDiagnosticAsync(testCode, new[] { expected1, expected2 }, CancellationToken.None).ConfigureAwait(false);

            var fixedCode = @"
namespace RoslynSandbox
{
    public sealed class Foo
    {
        public Foo()
        {
        }

        public int Bar
        {
            get { return this.Bar; }
            set { this.Bar = value; }
        }

        public class Nested
        {
            
            public int Value2 { get; private set; }

            public int Value1 { get; set; }
        }
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
