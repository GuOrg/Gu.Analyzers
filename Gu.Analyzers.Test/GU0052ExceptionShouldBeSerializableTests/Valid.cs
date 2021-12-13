namespace Gu.Analyzers.Test.GU0052ExceptionShouldBeSerializableTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal static class Valid
    {
        private static readonly GU0052ExceptionShouldBeSerializable Analyzer = new();

        [Test]
        public static void WhenNoBaseClass()
        {
            var code = @"
namespace N
{
    public class C
    {
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSerializable()
        {
            var code = @"
namespace N
{
    using System;

    [Serializable]
    public class C : Exception
    {
        public C()
            : base(string.Empty)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSerializableAndObsoleteSameList()
        {
            var code = @"
namespace N
{
    using System;

    [Serializable, Obsolete]
    public class C : Exception
    {
        public C()
            : base(string.Empty)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void WhenSerializableAndObsoleteDifferentLists()
        {
            var code = @"
namespace N
{
    using System;

    [Serializable]
    [Obsolete]
    public class C : Exception
    {
        public C()
            : base(string.Empty)
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }

        [Test]
        public static void ExtendedWithAttribute()
        {
            var code = @"
namespace N
{
    using System;
    
    [Serializable]
    public class C1 : Exception
    {
        public C1()
        : base(string.Empty)
        {
        }
    }

    [Serializable]
    public class C2 : C1
    {
        public C2()
        : base()
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, code);
        }
    }
}
