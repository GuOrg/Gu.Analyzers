// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    public class TestMethodAnalyzerBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.TestMethodAnalyzer());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void RunOnGuAnalyzersProject()
        {
            Benchmark.Run();
        }
    }
}
