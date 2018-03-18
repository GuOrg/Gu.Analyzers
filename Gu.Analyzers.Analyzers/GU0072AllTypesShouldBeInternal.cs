namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0072AllTypesShouldBeInternal : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0072";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "All types should be internal.",
            messageFormat: "All types should be internal.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.DisabledByDefault,
            description: "All types should be internal.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            // add check here
        }
    }
}
