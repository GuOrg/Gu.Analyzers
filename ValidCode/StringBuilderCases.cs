// ReSharper disable All
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

        internal void AllBenchmarks()
        {
            var builder = new StringBuilder();
            builder.AppendLine("// ReSharper disable RedundantNameQualifier")
                   .AppendLine($"namespace {this.GetType().Namespace}")
                   .AppendLine("{")
                   .AppendLine("    [BenchmarkDotNet.Attributes.MemoryDiagnoser]")
                   .AppendLine("    public class AllBenchmarks")
                   .AppendLine("    {");
            foreach (var analyzer in new[] { "1" })
            {
                builder.AppendLine(
                           $"        private static readonly Gu.Roslyn.Asserts.Benchmark {analyzer.GetType().Name}Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.ValidCodeProject, new {analyzer.GetType().FullName}());")
                       .AppendLine();
            }

            foreach (var analyzer in new[] { "1" })
            {
                builder.AppendLine($"        [BenchmarkDotNet.Attributes.Benchmark]")
                       .AppendLine($"        public void {analyzer.GetType().Name}()")
                       .AppendLine("        {")
                       .AppendLine($"            {analyzer.GetType().Name}Benchmark.Run();")
                       .AppendLine("        }");
                if (!ReferenceEquals(analyzer, analyzer))
                {
                    builder.AppendLine();
                }
            }

            builder.AppendLine("    }")
                   .AppendLine("}");

            _ = builder.ToString();
        }
    }
}
