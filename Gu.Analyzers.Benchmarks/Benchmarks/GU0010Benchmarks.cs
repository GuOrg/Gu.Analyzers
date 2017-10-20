namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    public class GU0010Benchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0010DoNotAssignSameValue());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void RunOnGuAnalyzersProject()
        {
            Benchmark.Run();
        }
    }
}
