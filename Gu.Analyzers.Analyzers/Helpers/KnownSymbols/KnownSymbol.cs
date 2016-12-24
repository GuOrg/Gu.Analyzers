// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
namespace Gu.Analyzers
{
    internal static class KnownSymbol
    {
        internal static readonly QualifiedType Object = Create("System.Object");
        internal static readonly QualifiedType Boolean = Create("System.Boolean");
        internal static readonly StringType String = new StringType();
        internal static readonly QualifiedType Tuple = Create("System.Tuple");
        internal static readonly QualifiedType SerializableAttribute = Create("System.SerializableAttribute");
        internal static readonly QualifiedType NonSerializedAttribute = Create("System.NonSerializedAttribute");
        internal static readonly DisposableType IDisposable = new DisposableType();
        internal static readonly QualifiedType ArgumentException = Create("System.ArgumentException");
        internal static readonly QualifiedType ArgumentNullException = Create("System.ArgumentNullException");
        internal static readonly QualifiedType ArgumentOutOfRangeException = Create("System.ArgumentOutOfRangeException");
        internal static readonly QualifiedType EventHandler = Create("System.EventHandler");

        internal static readonly StringBuilderType StringBuilder = new StringBuilderType();

        internal static readonly TaskType Task = new TaskType();
        internal static readonly TextReaderType TextReader = new TextReaderType();

        internal static readonly QualifiedType Enumerable = Create("System.Linq.Enumerable");
        internal static readonly QualifiedType Expression = Create("System.Linq.Expressions.Expression");
        internal static readonly DependencyPropertyType DependencyProperty = new DependencyPropertyType();

        internal static readonly PasswordBoxType PasswordBox = new PasswordBoxType();

        private static QualifiedType Create(string qualifiedName)
        {
            return new QualifiedType(qualifiedName);
        }
    }
}