namespace Gu.Analyzers
{
    using System.Collections.Immutable;
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
            context.RegisterSyntaxNodeAction(HandleEventField, SyntaxKind.EventFieldDeclaration);
            context.RegisterSyntaxNodeAction(HandleField, SyntaxKind.FieldDeclaration);
        }

        private static void HandleEventField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is EventFieldDeclarationSyntax eventFieldDeclaration &&
                context.ContainingSymbol is IEventSymbol eventSymbol &&
                HasSerializableAttribute(eventSymbol.ContainingType))
            {
                foreach (var attributeList in eventFieldDeclaration.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        if ((attribute.Name as IdentifierNameSyntax)?.Identifier.ValueText.Contains("NonSerialized") ==
                            true)
                        {
                            var attributeType =
                                context.SemanticModel.GetSymbolSafe(attribute, context.CancellationToken)
                                       ?.ContainingType;
                            if (attributeType != KnownSymbol.NonSerializedAttribute)
                            {
                                continue;
                            }

                            return;
                        }
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, eventFieldDeclaration.GetLocation()));
            }
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var field = (IFieldSymbol)context.ContainingSymbol;
            if (!field.Type.Is(KnownSymbol.EventHandler))
            {
                return;
            }

            var type = context.ContainingSymbol.ContainingType;
            if (!HasSerializableAttribute(type))
            {
                return;
            }

            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
            foreach (var attributeList in fieldDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if ((attribute.Name as IdentifierNameSyntax)?.Identifier.ValueText.Contains("NonSerialized") == true)
                    {
                        var attributeType = context.SemanticModel.GetSymbolSafe(attribute, context.CancellationToken)
                                                                ?.ContainingType;
                        if (attributeType != KnownSymbol.NonSerializedAttribute)
                        {
                            continue;
                        }

                        return;
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, fieldDeclaration.GetLocation()));
        }

        private static bool HasSerializableAttribute(INamedTypeSymbol type)
        {
            return type.GetAttributes()
                       .TryGetFirst(x => x.AttributeClass == KnownSymbol.SerializableAttribute, out AttributeData _);
        }
    }
}