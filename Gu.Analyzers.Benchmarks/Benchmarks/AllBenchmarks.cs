// ReSharper disable RedundantNameQualifier
namespace Gu.Analyzers.Benchmarks
{
    [BenchmarkDotNet.Attributes.MemoryDiagnoser]
    public class AllBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark ArgumentAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ArgumentAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ArgumentListAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ArgumentListAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark AssertAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.AssertAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark BinaryExpressionAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.BinaryExpressionAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ClassDeclarationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ClassDeclarationAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ConstructorAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ConstructorAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark DocsAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.DocsAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ExceptionAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ExceptionAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark IdentifierNameAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.IdentifierNameAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark MethodGroupAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.MethodGroupAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ObjectCreationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ObjectCreationAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark ParameterAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.ParameterAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark PropertyDeclarationAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.PropertyDeclarationAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark SimpleAssignmentAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.SimpleAssignmentAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark StringLiteralExpressionAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.StringLiteralExpressionAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark TestMethodAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.TestMethodAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark VariableDeclaratorAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.VariableDeclaratorAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark WhenAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.WhenAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0007PreferInjectingBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0007PreferInjecting());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0011DoNotIgnoreReturnValueBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0011DoNotIgnoreReturnValue());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0020SortPropertiesBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0020SortProperties());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0022UseGetOnlyBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0022UseGetOnly());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0023StaticMemberOrderAnalyzerBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0023StaticMemberOrderAnalyzer());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0050IgnoreEventsWhenSerializingBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0050IgnoreEventsWhenSerializing());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0051XmlSerializerNotCachedBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0051XmlSerializerNotCached());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0052ExceptionShouldBeSerializableBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0052ExceptionShouldBeSerializable());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0060EnumMemberValueConflictsWithAnotherBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0060EnumMemberValueConflictsWithAnother());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0061EnumMemberValueOutOfRangeBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0061EnumMemberValueOutOfRange());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0070DefaultConstructedValueTypeWithNoUsefulDefaultBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0070DefaultConstructedValueTypeWithNoUsefulDefault());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0071ForeachImplicitCastBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0071ForeachImplicitCast());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0072AllTypesShouldBeInternalBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0072AllTypesShouldBeInternal());

        private static readonly Gu.Roslyn.Asserts.Benchmark GU0073MemberShouldBeInternalBenchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0073MemberShouldBeInternal());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ArgumentAnalyzer()
        {
            ArgumentAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ArgumentListAnalyzer()
        {
            ArgumentListAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void AssertAnalyzer()
        {
            AssertAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void BinaryExpressionAnalyzer()
        {
            BinaryExpressionAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ClassDeclarationAnalyzer()
        {
            ClassDeclarationAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ConstructorAnalyzer()
        {
            ConstructorAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void DocsAnalyzer()
        {
            DocsAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ExceptionAnalyzer()
        {
            ExceptionAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void IdentifierNameAnalyzer()
        {
            IdentifierNameAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void MethodGroupAnalyzer()
        {
            MethodGroupAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ObjectCreationAnalyzer()
        {
            ObjectCreationAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void ParameterAnalyzer()
        {
            ParameterAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void PropertyDeclarationAnalyzer()
        {
            PropertyDeclarationAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void SimpleAssignmentAnalyzer()
        {
            SimpleAssignmentAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void StringLiteralExpressionAnalyzer()
        {
            StringLiteralExpressionAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void TestMethodAnalyzer()
        {
            TestMethodAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void VariableDeclaratorAnalyzer()
        {
            VariableDeclaratorAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void WhenAnalyzer()
        {
            WhenAnalyzerBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0007PreferInjecting()
        {
            GU0007PreferInjectingBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0011DoNotIgnoreReturnValue()
        {
            GU0011DoNotIgnoreReturnValueBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0020SortProperties()
        {
            GU0020SortPropertiesBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0022UseGetOnly()
        {
            GU0022UseGetOnlyBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0023StaticMemberOrderAnalyzer()
        {
            GU0023StaticMemberOrderAnalyzerBenchmark.Run();
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
        public void GU0052ExceptionShouldBeSerializable()
        {
            GU0052ExceptionShouldBeSerializableBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0060EnumMemberValueConflictsWithAnother()
        {
            GU0060EnumMemberValueConflictsWithAnotherBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0061EnumMemberValueOutOfRange()
        {
            GU0061EnumMemberValueOutOfRangeBenchmark.Run();
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
        public void GU0072AllTypesShouldBeInternal()
        {
            GU0072AllTypesShouldBeInternalBenchmark.Run();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void GU0073MemberShouldBeInternal()
        {
            GU0073MemberShouldBeInternalBenchmark.Run();
        }
    }
}
