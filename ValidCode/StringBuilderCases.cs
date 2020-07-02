namespace ValidCode
{
    using System;
    using System.Text;

    internal class StringBuilderCases
    {
        internal void M1(string typeName)
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine($"Found more than one match for {typeName}");
        }

        internal void M2(string typeName)
        {
            try
            {
            }
            catch (Exception)
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine($"Found more than one match for {typeName}");
            }
        }
    }
}
