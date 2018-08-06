using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Gu.Roslyn.Asserts;

[assembly: AssemblyTitle("Gu.Analyzers.Test")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(false)]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: MetadataReference(typeof(object), new[] { "global", "mscorlib" })]
[assembly: MetadataReference(typeof(System.Diagnostics.Debug), new[] { "global", "System" })]
[assembly: TransitiveMetadataReferences(typeof(Microsoft.CodeAnalysis.Editing.DocumentEditor))]
[assembly: TransitiveMetadataReferences(typeof(System.Reactive.Concurrency.DispatcherScheduler))]
[assembly: MetadataReferences(
    typeof(System.Linq.Enumerable),
    typeof(System.Net.WebClient),
    typeof(System.Drawing.Bitmap),
    typeof(System.Data.Common.DbConnection),
    typeof(System.Xml.Serialization.XmlSerializer),
    typeof(System.Runtime.Serialization.DataContractSerializer),
    typeof(System.Windows.Media.Brush),
    typeof(System.Windows.Controls.Control),
    typeof(System.Windows.Media.Matrix),
    typeof(System.Xaml.XamlLanguage),
    typeof(System.Collections.Immutable.ImmutableArray),
    typeof(Ninject.StandardKernel),
    typeof(Gu.Roslyn.AnalyzerExtensions.Cache),
    typeof(Gu.Roslyn.CodeFixExtensions.CodeStyle),
    typeof(NUnit.Framework.Assert),
    typeof(AnalyzerAssert),
    typeof(Moq.Mock))]
