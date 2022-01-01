// ReSharper disable InconsistentNaming
namespace Gu.Analyzers;

using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;

internal static class KnownSymbols
{
    internal static readonly QualifiedType Void = Create("System.Void");
    internal static readonly QualifiedType Object = Create("System.Object", "object");
    internal static readonly QualifiedType Boolean = Create("System.Boolean", "bool");
    internal static readonly QualifiedType Int32 = Create("System.Int32", "int");
    internal static readonly QualifiedType Int64 = Create("System.Int64", "long");
    internal static readonly StringType String = new();
    internal static readonly QualifiedType Tuple = Create("System.Tuple");
    internal static readonly QualifiedType Type = Create("System.Type");
    internal static readonly QualifiedType Guid = Create("System.Guid");
    internal static readonly QualifiedType Exception = Create("System.Exception");
    internal static readonly QualifiedType DateTime = Create("System.DateTime");
    internal static readonly QualifiedType SerializableAttribute = Create("System.SerializableAttribute");
    internal static readonly QualifiedType NonSerializedAttribute = Create("System.NonSerializedAttribute");
    internal static readonly QualifiedType ArgumentException = Create("System.ArgumentException");
    internal static readonly QualifiedType ArgumentNullException = Create("System.ArgumentNullException");
    internal static readonly QualifiedType ArgumentOutOfRangeException = Create("System.ArgumentOutOfRangeException");
    internal static readonly QualifiedType EventHandler = Create("System.EventHandler");

    internal static readonly QualifiedType IDictionary = Create("System.Collections.IDictionary");

    internal static readonly QualifiedType FlagsAttribute = Create("System.FlagsAttribute");

    internal static readonly StringBuilderType StringBuilder = new();

    internal static readonly IEnumerableType IEnumerable = new();
    internal static readonly TaskType Task = new();
    internal static readonly QualifiedType CancellationToken = new(typeof(CancellationToken).FullName!);
    internal static readonly QualifiedType TaskOfT = new("System.Threading.Tasks.Task`1");
    internal static readonly XmlSerializerType XmlSerializer = new();

    internal static readonly DependencyPropertyType DependencyProperty = new();
    internal static readonly DependencyObjectType DependencyObject = new();

    internal static readonly NUnitAssertType NUnitAssert = new();
    internal static readonly QualifiedType NUnitTestAttribute = new("NUnit.Framework.TestAttribute");
    internal static readonly QualifiedType NUnitTestCaseAttribute = new("NUnit.Framework.TestCaseAttribute");
    internal static readonly QualifiedType NUnitTestCaseSourceAttribute = new("NUnit.Framework.TestCaseSourceAttribute");
    internal static readonly XunitAssertType XunitAssert = new();
    internal static readonly QualifiedType MoqMockOfT = new("Moq.Mock`1");
    internal static readonly QualifiedType MoqIFluentInterface = new("Moq.IFluentInterface");
    internal static readonly QualifiedType NinjectIFluentSyntax = new("Ninject.Syntax.IFluentSyntax");
    internal static readonly QualifiedType GuInjectKernel = new("Gu.Inject.Kernel");
    internal static readonly QualifiedType GuInjectKernelExtensions = new("Gu.Inject.KernelExtensions");

    private static QualifiedType Create(string qualifiedName, string? alias = null)
    {
        return new QualifiedType(qualifiedName, alias);
    }
}