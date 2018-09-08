// ReSharper disable All
namespace ValidCode
{
    using NUnit.Framework;

    internal class Tests
    {
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.MinValue)]
        [TestCase(int.MinValue)]
        [TestCase(-1.2)]
        [TestCase(-1)]
        [TestCase(-0.1)]
        [TestCase(-1.2E-123)]
        [TestCase(-0.0)]
        [TestCase(0.0)]
        [TestCase(1.2E-123)]
        [TestCase(0.1)]
        [TestCase(1)]
        [TestCase(1.2)]
        [TestCase(1.2E123)]
        [TestCase(int.MaxValue)]
        [TestCase(double.MaxValue)]
        [TestCase(double.NaN)]
        public void WithDouble(double value)
        {
            Assert.AreEqual(value, 1* value);
        }
    }
}
