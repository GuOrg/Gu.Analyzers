namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DocsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0100WrongDocs);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.XmlCrefAttribute);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is XmlCrefAttributeSyntax attribute &&
                attribute.Parent is XmlEmptyElementSyntax emptyElement &&
                emptyElement.Parent is XmlElementSyntax candidate &&
                candidate.HasLocalName("param") &&
                candidate.TryGetNameAttribute(out var nameAttribute) &&
                context.ContainingSymbol is IMethodSymbol method &&
                method.TryFindParameter(nameAttribute.Identifier.Identifier.ValueText, out var parameter) &&
                context.SemanticModel.TryGetType(attribute.Cref, context.CancellationToken, out var type) &&
                !type.Equals(parameter.Type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.GU0100WrongDocs,
                        attribute.Cref.GetLocation()));
            }
        }
    }
}
