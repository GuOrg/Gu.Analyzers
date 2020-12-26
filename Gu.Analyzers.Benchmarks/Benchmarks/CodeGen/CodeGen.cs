﻿namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Gu.Roslyn.Asserts;

    using Microsoft.CodeAnalysis.Diagnostics;

    using NUnit.Framework;

    [Explicit("Script")]
    public class CodeGen
    {
        public static string ProjectDirectory { get; } = ProjectFile.Find("Gu.Analyzers.Benchmarks.csproj").DirectoryName;

        public static string BenchmarksDirectory { get; } = Path.Combine(ProjectDirectory, "Benchmarks");

        private static IReadOnlyList<DiagnosticAnalyzer> AllAnalyzers { get; } = typeof(KnownSymbols)
            .Assembly
            .GetTypes()
            .Where(typeof(DiagnosticAnalyzer).IsAssignableFrom)
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
            .ToArray();

        [TestCaseSource(nameof(AllAnalyzers))]
        public void AnalyzersBenchmark(DiagnosticAnalyzer analyzer)
        {
            var expectedName = analyzer.GetType().Name + "Benchmarks";
            var fileName = Path.Combine(BenchmarksDirectory, expectedName + ".cs");
            var code = new StringBuilder().AppendLine("// ReSharper disable RedundantNameQualifier")
                                          .AppendLine("// ReSharper disable InconsistentNaming")
                                          .AppendLine($"namespace {this.GetType().Namespace}")
                                          .AppendLine("{")
                                          .AppendLine("    [BenchmarkDotNet.Attributes.MemoryDiagnoser]")
                                          .AppendLine($"    public class {expectedName}")
                                          .AppendLine("    {")
                                          .AppendLine($"        private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new {analyzer.GetType().FullName}());")
                                          .AppendLine()
                                          .AppendLine("        [BenchmarkDotNet.Attributes.Benchmark]")
                                          .AppendLine("        public void RunOnGuAnalyzersProject()")
                                          .AppendLine("        {")
                                          .AppendLine("            Benchmark.Run();")
                                          .AppendLine("        }")
                                          .AppendLine("    }")
                                          .AppendLine("}")
                                          .ToString();
            if (!File.Exists(fileName) ||
                !CodeComparer.Equals(File.ReadAllText(fileName), code))
            {
                File.WriteAllText(fileName, code);
                Assert.Fail();
            }
        }

        [Test]
        public void AllBenchmarks()
        {
            var fileName = Path.Combine(BenchmarksDirectory, "AllBenchmarks.cs");
            var builder = new StringBuilder();
            builder.AppendLine("// ReSharper disable RedundantNameQualifier")
                   .AppendLine($"namespace {this.GetType().Namespace}")
                   .AppendLine("{")
                   .AppendLine("    [BenchmarkDotNet.Attributes.MemoryDiagnoser]")
                   .AppendLine("    public class AllBenchmarks")
                   .AppendLine("    {");
            foreach (var analyzer in AllAnalyzers)
            {
                builder.AppendLine(
                           $"        private static readonly Gu.Roslyn.Asserts.Benchmark {analyzer.GetType().Name}Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new {analyzer.GetType().FullName}());")
                       .AppendLine();
            }

            foreach (var analyzer in AllAnalyzers)
            {
                builder.AppendLine($"        [BenchmarkDotNet.Attributes.Benchmark]")
                       .AppendLine($"        public void {analyzer.GetType().Name}()")
                       .AppendLine("        {")
                       .AppendLine($"            {analyzer.GetType().Name}Benchmark.Run();")
                       .AppendLine("        }");
                if (!ReferenceEquals(analyzer, AllAnalyzers[AllAnalyzers.Count - 1]))
                {
                    builder.AppendLine();
                }
            }

            builder.AppendLine("    }")
                   .AppendLine("}");

            var code = builder.ToString();
            if (!File.Exists(fileName) ||
                !CodeComparer.Equals(File.ReadAllText(fileName), code))
            {
                File.WriteAllText(fileName, code);
                Assert.Fail();
            }
        }

        [Test]
        public void BenchmarksDirectoryExists()
        {
            Assert.AreEqual(true, Directory.Exists(BenchmarksDirectory), BenchmarksDirectory);
        }
    }
}
