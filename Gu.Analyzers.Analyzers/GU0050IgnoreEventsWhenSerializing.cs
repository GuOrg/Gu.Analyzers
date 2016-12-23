namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GU0050IgnoreEventsWhenSerializing : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0050";

        private const string Title = "Ignore events when serializing.";

        private const string MessageFormat = "Ignore events when serializing.";

        private const string Description = "Ignore events when serializing.";

        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: Description,
            helpLinkUri: HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleEvent, SyntaxKind.EventFieldDeclaration);
            context.RegisterSyntaxNodeAction(HandleEvent, SyntaxKind.EventDeclaration);
        }

        private static void HandleEvent(SyntaxNodeAnalysisContext context)
        {
            var eventSymbol = (IEventSymbol)context.ContainingSymbol;
            var type = eventSymbol.ContainingType;
            if (type.GetAttributes().Any(x => x.AttributeClass == KnownSymbol.SerializableAttribute))
            {
                var attributes = eventSymbol.GetAttributes();
                if (attributes.Any(x => x.AttributeClass == KnownSymbol.NonSerializedAttribute))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }
    }
}