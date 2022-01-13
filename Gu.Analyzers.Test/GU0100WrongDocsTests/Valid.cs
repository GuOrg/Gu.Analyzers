namespace Gu.Analyzers.Test.GU0100WrongDocsTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static class Valid
{
    private static readonly DocsAnalyzer Analyzer = new();

    [TestCase("StringBuilder")]
    [TestCase("System.Text.StringBuilder")]
    public static void WhenCorrect(string cref)
    {
        var code = @"
#pragma warning disable CS8019
namespace N
{
    using System.Text;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""builder"">The <see cref=""StringBuilder""/>.</param>
        public void M(StringBuilder builder)
        {
        }
    }
}".AssertReplace("StringBuilder", cref);
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("List{int}")]
    [TestCase("List{Int32}")]
    [TestCase("List{System.Int32}")]
    [TestCase("System.Collections.Generic.List{int}")]
    [TestCase("System.Collections.Generic.List{Int32}")]
    [TestCase("System.Collections.Generic.List{System.Int32}")]
    public static void WhenListOfInt(string cref)
    {
        var code = @"
#pragma warning disable CS8019
namespace N
{
    using System;
    using System.Collections.Generic;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""list"">The <see cref=""List{int}""/>.</param>
        public void M(List<int> list)
        {
        }
    }
}".AssertReplace("List{int}", cref);
        RoslynAssert.Valid(Analyzer, code);
    }

    [TestCase("List{C}")]
    [TestCase("List{N.C}")]
    [TestCase("System.Collections.Generic.List{C}")]
    [TestCase("System.Collections.Generic.List{N.C}")]
    public static void WhenListOfC(string cref)
    {
        var code = @"
#pragma warning disable CS8019
namespace N
{
    using System;
    using System.Collections.Generic;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""list"">The <see cref=""List{C}""/>.</param>
        public void M(List<C> list)
        {
        }
    }
}".AssertReplace("List{C}", cref);
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void WhenKeyValuePair()
    {
        var code = @"
namespace N
{
    using System.Text;
    using System.Collections.Generic;
    using System;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""list"">The <see cref=""KeyValuePair{Type, StringBuilder}""/>.</param>
        public void M(KeyValuePair<Type, StringBuilder> kvp)
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void WhenOtherText()
    {
        var code = @"
namespace N
{
    using System.Text;

    class C
    {
        /// <summary>
        /// Text.
        /// </summary>
        /// <param name=""builder"">For creating a <see cref=""string""/>.</param>
        public void M(StringBuilder builder)
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
