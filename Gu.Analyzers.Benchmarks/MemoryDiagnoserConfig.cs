[assembly: BenchmarkDotNet.Attributes.Config(typeof(Gu.Analyzers.Benchmarks.MemoryDiagnoserConfig))]
namespace Gu.Analyzers.Benchmarks
{
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;

    public class MemoryDiagnoserConfig : ManualConfig
    {
        public MemoryDiagnoserConfig()
        {
            this.Add(new MemoryDiagnoser());
        }
    }
}