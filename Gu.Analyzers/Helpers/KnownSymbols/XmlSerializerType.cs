namespace Gu.Analyzers;

using Gu.Roslyn.AnalyzerExtensions;

// ReSharper disable once InconsistentNaming
internal class XmlSerializerType : QualifiedType
{
    internal XmlSerializerType()
        : base("System.Xml.Serialization.XmlSerializer")
    {
    }
}
