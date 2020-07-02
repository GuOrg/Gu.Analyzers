namespace ValidCode
{
    using System.Text;

    public class StringBuilderCases
    {
        public void M(string typeName)
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine($"Found more than one match for {typeName}");
        }
    }
}
