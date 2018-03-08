// ReSharper disable RedundantNameQualifier
namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    public class AllBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark GU0003CtorParameterNamesShouldMatchBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0003CtorParameterNamesShouldMatch());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0004AssignAllReadOnlyMembersBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0004AssignAllReadOnlyMembers());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0006UseNameofBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0006UseNameof());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0007PreferInjectingBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0007PreferInjecting());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0008AvoidRelayPropertiesBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0008AvoidRelayProperties());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0009UseNamedParametersForBooleansBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0009UseNamedParametersForBooleans());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0010DoNotAssignSameValueBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0010DoNotAssignSameValue());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0011DontIgnoreReturnValueBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0011DontIgnoreReturnValue());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0012NullCheckParameterBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0012NullCheckParameter());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0020SortPropertiesBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0020SortProperties());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0021CalculatedPropertyAllocatesBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0021CalculatedPropertyAllocates());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0022UseGetOnlyBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0022UseGetOnly());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0050IgnoreEventsWhenSerializingBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0050IgnoreEventsWhenSerializing());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0051XmlSerializerNotCachedBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0051XmlSerializerNotCached());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0060EnumMemberValueConflictsWithAnotherBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0060EnumMemberValueConflictsWithAnother());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0070DefaultConstructedValueTypeWithNoUsefulDefaultBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0070DefaultConstructedValueTypeWithNoUsefulDefault());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0071ForeachImplicitCastBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0071ForeachImplicitCast());

        private static readonly Gu.Roslyn.Asserts.Benchmark ArgumentListAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ArgumentListAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ObjectCreationExpressionAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ObjectCreationExpressionAnalyzer());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0003CtorParameterNamesShouldMatch()
        {
            GU0003CtorParameterNamesShouldMatchBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0004AssignAllReadOnlyMembers()
        {
            GU0004AssignAllReadOnlyMembersBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0006UseNameof()
        {
            GU0006UseNameofBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0007PreferInjecting()
        {
            GU0007PreferInjectingBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0008AvoidRelayProperties()
        {
            GU0008AvoidRelayPropertiesBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0009UseNamedParametersForBooleans()
        {
            GU0009UseNamedParametersForBooleansBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0010DoNotAssignSameValue()
        {
            GU0010DoNotAssignSameValueBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0011DontIgnoreReturnValue()
        {
            GU0011DontIgnoreReturnValueBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0012NullCheckParameter()
        {
            GU0012NullCheckParameterBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0020SortProperties()
        {
            GU0020SortPropertiesBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0021CalculatedPropertyAllocates()
        {
            GU0021CalculatedPropertyAllocatesBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0022UseGetOnly()
        {
            GU0022UseGetOnlyBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0050IgnoreEventsWhenSerializing()
        {
            GU0050IgnoreEventsWhenSerializingBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0051XmlSerializerNotCached()
        {
            GU0051XmlSerializerNotCachedBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0060EnumMemberValueConflictsWithAnother()
        {
            GU0060EnumMemberValueConflictsWithAnotherBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0070DefaultConstructedValueTypeWithNoUsefulDefault()
        {
            GU0070DefaultConstructedValueTypeWithNoUsefulDefaultBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0071ForeachImplicitCast()
        {
            GU0071ForeachImplicitCastBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ArgumentListAnalyzer()
        {
            ArgumentListAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ObjectCreationExpressionAnalyzer()
        {
            ObjectCreationExpressionAnalyzerBenchmark.Run();
        }
    }
}
