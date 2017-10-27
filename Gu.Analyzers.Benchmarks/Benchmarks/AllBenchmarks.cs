// ReSharper disable InconsistentNaming
// ReSharper disable RedundantNameQualifier
namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    public class AllBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark GU0001 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0001NameArguments());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0002 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0002NamedArgumentPositionMatches());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0003 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0003CtorParameterNamesShouldMatch());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0004 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0004AssignAllReadOnlyMembers());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0005 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0005ExceptionArgumentsPositions());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0006 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0006UseNameof());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0007 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0007PreferInjecting());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0008 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0008AvoidRelayProperties());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0009 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0009UseNamedParametersForBooleans());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0010 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0010DoNotAssignSameValue());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0011 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0011DontIgnoreReturnValue());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0020 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0020SortProperties());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0021 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0021CalculatedPropertyAllocates());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0022 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0022UseGetOnly());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0050 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0050IgnoreEventsWhenSerializing());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0051 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0051XmlSerializerNotCached());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0060 = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0060EnumMemberValueConflictsWithAnother());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0001NameArguments()
        {
            GU0001.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0002NamedArgumentPositionMatches()
        {
            GU0002.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0003CtorParameterNamesShouldMatch()
        {
            GU0003.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0004AssignAllReadOnlyMembers()
        {
            GU0004.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0005ExceptionArgumentsPositions()
        {
            GU0005.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0006UseNameof()
        {
            GU0006.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0007PreferInjecting()
        {
            GU0007.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0008AvoidRelayProperties()
        {
            GU0008.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0009UseNamedParametersForBooleans()
        {
            GU0009.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0010DoNotAssignSameValue()
        {
            GU0010.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0011DontIgnoreReturnValue()
        {
            GU0011.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0020SortProperties()
        {
            GU0020.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0021CalculatedPropertyAllocates()
        {
            GU0021.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0022UseGetOnly()
        {
            GU0022.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0050IgnoreEventsWhenSerializing()
        {
            GU0050.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0051XmlSerializerNotCached()
        {
            GU0051.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0060EnumMemberValueConflictsWithAnother()
        {
            GU0060.Run();
        }
    }
}
