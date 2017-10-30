using System.Collections.Generic;

namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0070DefaultConstructedValueTypeWithNoUsefulDefault : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0070";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Default-constructed value type with no no useful default",
            messageFormat: "Default constructed value type was created, which is likely not what was intended.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Types declared with struct must have a default constructor, even if there is no semantically sensible default value for that type. Examples include System.Guid and System.DateTime.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ObjectCreationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if(context.IsExcludedFromAnalysis())
            {
                return;
            }

            if(context.Node is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.TryGetConstructor(KnownSymbol.Guid, context.SemanticModel, context.CancellationToken, out var ctor) &&
                ctor.Parameters.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, objectCreation.GetLocation()));
            }
        }
    }
}