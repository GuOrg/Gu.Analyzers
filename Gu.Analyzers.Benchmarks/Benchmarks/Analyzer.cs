namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using BenchmarkDotNet.Attributes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    public abstract class Analyzer
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> analyzer;
        private readonly Project project;
        private readonly CompilationWithAnalyzers compilationWithAnalyzers;

        protected Analyzer(DiagnosticAnalyzer analyzer)
        {
            this.analyzer = ImmutableArray.Create(analyzer);
            this.project = CreateProject("C:\\Git\\Gu.Analyzers\\Gu.Analyzers.Analyzers\\Gu.Analyzers.Analyzers.csproj", analyzer);
            var compilation = this.project.GetCompilationAsync(CancellationToken.None).Result;
            this.compilationWithAnalyzers = compilation.WithAnalyzers(
                this.analyzer,
                this.project.AnalyzerOptions,
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<object> GetAnalyzerDiagnosticsAsync()
        {
            return await this.compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None)
                                                      .ConfigureAwait(false);
        }

        private static Project CreateProject(string projFile, DiagnosticAnalyzer analyzer)
        {
            var projectId = ProjectId.CreateNewId("Gu.Analyzers");
            var solution = CreateSolution(projectId);

            var root = XDocument.Parse(File.ReadAllText(projFile));
            var directory = Path.GetDirectoryName(projFile);
            foreach (var compile in root.Descendants("Compile"))
            {
                var csFile = Path.Combine(directory, compile.Attribute("Include").Value);
                var documentId = DocumentId.CreateNewId(projectId, debugName: csFile);
                using (var stream = File.OpenRead(csFile))
                {
                    solution = solution.AddDocument(documentId, csFile, SourceText.From(stream));
                }
            }

            var project = solution.GetProject(projectId);
            return ApplyCompilationOptions(project, analyzer);
        }

        private static Solution CreateSolution(ProjectId projectId)
        {
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "Gu.Analyzers", "Gu.Analyzers", LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId, compilationOptions)
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location).WithAliases(ImmutableArray.Create("global", "corlib")))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location).WithAliases(ImmutableArray.Create("global", "system")))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(WebClient).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Xml.Serialization.XmlSerializer).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location));
            var parseOptions = solution.GetProject(projectId).ParseOptions;
            return solution.WithProjectParseOptions(projectId, parseOptions.WithDocumentationMode(DocumentationMode.Diagnose));
        }

        private static Project ApplyCompilationOptions(Project project, DiagnosticAnalyzer analyzer)
        {
            // update the project compilation options
            var diagnostics = ImmutableDictionary.CreateRange(
                analyzer.SupportedDiagnostics.Select(x => new KeyValuePair<string, ReportDiagnostic>(x.Id, ReportDiagnostic.Warn)));

            var modifiedSpecificDiagnosticOptions = diagnostics.SetItems(project.CompilationOptions.SpecificDiagnosticOptions);
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);

            var solution = project.Solution.WithProjectCompilationOptions(project.Id, modifiedCompilationOptions);
            return solution.GetProject(project.Id);
        }

    }
}
