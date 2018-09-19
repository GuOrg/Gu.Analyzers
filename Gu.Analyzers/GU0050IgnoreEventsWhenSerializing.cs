namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0050IgnoreEventsWhenSerializing : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0050";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Ignore events when serializing.",
            messageFormat: "Ignore events when serializing.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Ignore events when serializing.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => HandleEventField(c), SyntaxKind.EventFieldDeclaration);
            context.RegisterSyntaxNodeAction(c => HandleField(c), SyntaxKind.FieldDeclaration);
        }

        private static void HandleEventField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is EventFieldDeclarationSyntax eventFieldDeclaration &&
                context.ContainingSymbol is IEventSymbol eventSymbol &&
                HasSerializableAttribute(eventSymbol.ContainingType) &&
                !Attribute.TryFind(eventFieldDeclaration.AttributeLists, KnownSymbol.NonSerializedAttribute, context.SemanticModel, context.CancellationToken, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, eventFieldDeclaration.GetLocation()));
            }
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is FieldDeclarationSyntax fieldDeclaration &&
                context.ContainingSymbol is IFieldSymbol field &&
                field.Type.IsAssignableTo(KnownSymbol.EventHandler, context.Compilation) &&
                HasSerializableAttribute(field.ContainingType) &&
                !Attribute.TryFind(fieldDeclaration.AttributeLists, KnownSymbol.NonSerializedAttribute, context.SemanticModel, context.CancellationToken, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, fieldDeclaration.GetLocation()));
            }
        }

        private static bool HasSerializableAttribute(INamedTypeSymbol type)
        {
            return type.GetAttributes()
                       .TryFirst(x => x.AttributeClass == KnownSymbol.SerializableAttribute, out _);
        }
    }
}
