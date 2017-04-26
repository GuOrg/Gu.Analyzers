namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Microsoft.CodeAnalysis.Diagnostics;

    public abstract class Analyzer
    {
        private readonly CompilationWithAnalyzers compilation;

        protected Analyzer(DiagnosticAnalyzer analyzer)
        {
            var project = Factory.CreateProject(analyzer);
            this.compilation = project.GetCompilationAsync(CancellationToken.None)
                                      .Result
                                      .WithAnalyzers(
                                          ImmutableArray.Create(analyzer),
                                          project.AnalyzerOptions,
                                          CancellationToken.None);
        }

        [Benchmark]
        public async Task<object> GetAnalyzerDiagnosticsAsync()
        {
            return await this.compilation.GetAnalyzerDiagnosticsAsync(CancellationToken.None)
                             .ConfigureAwait(false);
        }
    }
}
