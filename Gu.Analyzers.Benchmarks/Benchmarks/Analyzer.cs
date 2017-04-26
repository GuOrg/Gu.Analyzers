namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    using System.Collections.Immutable;
    using System.Threading;
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
        public object GetAnalyzerDiagnosticsAsync()
        {
            return this.compilation.GetAnalyzerDiagnosticsAsync(CancellationToken.None).Result;
        }
    }
}
