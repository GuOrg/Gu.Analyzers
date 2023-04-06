// ReSharper disable All
namespace ValidCode
{
    using System;
    using System.Collections.Generic;

    internal class Recursion
    {
        private IDisposable? bar1;
        private IDisposable? bar2;

        internal Recursion()
        {
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(value);
            value = value;
        }

        internal IDisposable RecursiveProperty => this.RecursiveProperty;

        internal int Value1 => this.Value1;

        internal int Value2 => this.Value2;

        internal int Value3 => this.Value1;

        internal IDisposable RecursiveExpressionBodyProperty => this.RecursiveExpressionBodyProperty;

        internal IDisposable RecursiveStatementBodyProperty
        {
            get
            {
                return this.RecursiveStatementBodyProperty;
            }
        }

        internal IDisposable Value4
        {
            get
            {
                return this.Value4;
            }

            set
            {
                if (value == this.Value4)
                {
                    return;
                }

                this.Value4 = value;
            }
        }

        internal IDisposable Value5
        {
            get => this.Value5;
            set
            {
                if (value == this.Value5)
                {
                    return;
                }

                this.Value5 = value;
            }
        }

        internal IDisposable Value6
        {
            get => this.Value5;
            set
            {
                if (value == this.Value5)
                {
                    return;
                }

                this.Value5 = value;
            }
        }

        internal IDisposable? Bar1
        {
            get
            {
                return this.bar1;
            }

            set
            {
                if (Equals(value, this.bar1))
                {
                    return;
                }

                if (value != null && this.bar2 != null)
                {
                    this.Bar2 = null;
                }

                this.bar1 = value;
            }
        }

        internal IDisposable? Bar2
        {
            get
            {
                return this.bar2;
            }

            set
            {
                if (Equals(value, this.bar2))
                {
                    return;
                }

                if (value != null && this.bar1 != null)
                {
                    this.Bar1 = null;
                }

                this.bar2 = value;
            }
        }

        internal static bool RecursiveOut(out IDisposable value)
        {
            return RecursiveOut(out value);
        }

        internal static bool RecursiveOut(int foo, out IDisposable value)
        {
            return RecursiveOut(2, out value);
        }

        internal static bool RecursiveOut(double foo, out IDisposable? value)
        {
            value = null;
            return RecursiveOut(3.0, out value);
        }

        internal static bool RecursiveOut(string foo, out IDisposable? value)
        {
            if (foo is null)
            {
                return RecursiveOut(3.0, out value);
            }

            value = null;
            return true;
        }

        internal static bool RecursiveRef(ref IDisposable? value)
        {
            return RecursiveRef(ref value);
        }

        internal Disposable RecursiveMethod() => this.RecursiveMethod();

        internal void NotUsingRecursive()
        {
            var item1 = this.RecursiveProperty;
            var item2 = this.RecursiveMethod();
        }

        internal void UsingRecursive()
        {
            using (var item = new Disposable())
            {
            }

            using (var item = this.RecursiveProperty)
            {
            }

            using (this.RecursiveProperty)
            {
            }

            using (var item = this.RecursiveMethod())
            {
            }

            using (this.RecursiveMethod())
            {
            }
        }

        internal IDisposable RecursiveExpressionBodyMethod() => this.RecursiveExpressionBodyMethod();

        internal IDisposable RecursiveExpressionBodyMethod(int value) => this.RecursiveExpressionBodyMethod(value);

        internal IDisposable RecursiveStatementBodyMethod()
        {
            return this.RecursiveStatementBodyMethod();
        }

        internal IDisposable RecursiveStatementBodyMethod(int value)
        {
            return this.RecursiveStatementBodyMethod(value);
        }

        internal void Meh()
        {
            var value = this.RecursiveExpressionBodyProperty;
            value = this.RecursiveStatementBodyProperty;
            value = this.RecursiveExpressionBodyMethod();
            value = this.RecursiveExpressionBodyMethod(1);
            value = this.RecursiveStatementBodyMethod();
            value = this.RecursiveStatementBodyMethod(1);
            value = RecursiveStatementBodyMethodWithOptionalParameter(value);
            RecursiveOut(out value);
            RecursiveOut(1, out value);
            RecursiveOut(1.0, out value);
            RecursiveOut(string.Empty, out value);
            RecursiveRef(ref value);
            value = value;
        }

        private static IDisposable RecursiveStatementBodyMethodWithOptionalParameter(IDisposable value, IEnumerable<IDisposable>? values = null)
        {
            if (values is null)
            {
                return RecursiveStatementBodyMethodWithOptionalParameter(value, new[] { value });
            }

            return value;
        }
    }
}
