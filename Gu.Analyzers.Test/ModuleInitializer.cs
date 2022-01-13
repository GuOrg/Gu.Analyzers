namespace Gu.Analyzers.Test;

using System.Runtime.CompilerServices;

using Gu.Roslyn.Asserts;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Settings.Default = Settings.Default.WithMetadataReferences(
            MetadataReferences.Transitive(
                typeof(ModuleInitializer),
                typeof(System.Windows.Window),
                typeof(System.Reactive.Linq.Observable),
                typeof(Moq.Mock),
                typeof(Ninject.StandardKernel)));
    }
}
