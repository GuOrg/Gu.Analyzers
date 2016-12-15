namespace Gu.Analyzers.Test.Sandbox
{
    using System.Reflection;

    public class Foo
    {
        public void Bar()
        {
            var assembly = Assembly.Load(string.Empty);
        }
    }
}
