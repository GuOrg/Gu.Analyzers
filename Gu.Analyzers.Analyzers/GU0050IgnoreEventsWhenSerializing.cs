using System;

namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            context.RegisterSyntaxNodeAction(HandleEventField, SyntaxKind.EventFieldDeclaration);
            context.RegisterSyntaxNodeAction(HandleEvent, SyntaxKind.EventDeclaration);
        }

        private static void HandleEventField(SyntaxNodeAnalysisContext context)
        {
            var eventFieldDeclaration = (EventFieldDeclarationSyntax) context.Node;

            var type = context.ContainingSymbol.ContainingType;
            if (type.GetAttributes().Any(x => x.AttributeClass == KnownSymbol.SerializableAttribute))
            {
                var attributes = eventFieldDeclaration.AttributeLists.SelectMany(s => s.Attributes);
                var attributeInfo = attributes.Select(a => context.SemanticModel.GetSymbolInfo(a));
                if (attributeInfo.Any(x => x.Symbol.ContainingType == KnownSymbol.NonSerializedAttribute))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static void HandleEvent(SyntaxNodeAnalysisContext context)
        {
            var eventDeclaration = (EventDeclarationSyntax)context.Node;

            // ensure the event doesn't have the non-serialized attribute
            var type = context.ContainingSymbol.ContainingType;
            if (type.GetAttributes().Any(x => x.AttributeClass == KnownSymbol.SerializableAttribute))
            {
                var attributes = eventDeclaration.AttributeLists.SelectMany(s => s.Attributes);
                var attributeInfo = attributes.Select(a => context.SemanticModel.GetSymbolInfo(a));
                if (attributeInfo.Any(x => x.Symbol.ContainingType == KnownSymbol.NonSerializedAttribute))
                {
                    return;
                }
            }

            // ensure the backing field doesn't have it
            foreach (var accessor in eventDeclaration.AccessorList.Accessors)
            {
                var body = accessor?.Body;
                if (body == null) { continue; }

                var backingField = ((AssignmentExpressionSyntax)((ExpressionStatementSyntax)body.Statements.First()).Expression).Left;
                var backingFieldSymbol = context.SemanticModel.GetSymbolInfo(backingField).Symbol;
                if (backingFieldSymbol == null) { continue; }

                var attributes = backingFieldSymbol.GetAttributes();
                if (attributes.Any(x => x.AttributeClass == KnownSymbol.NonSerializedAttribute))
                {
                    return;
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
        }
    }
}