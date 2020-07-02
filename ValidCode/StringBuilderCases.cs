namespace ValidCode
{
    using System;
    using System.Text;

    public class StringBuilderCases
    {
        public void M1(string typeName)
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine($"Found more than one match for {typeName}");
        }

        public void M(string typeName)
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
