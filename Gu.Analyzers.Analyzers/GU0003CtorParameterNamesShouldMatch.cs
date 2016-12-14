namespace Gu.Analyzers
{
    using System.Collections.Immutable;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0003CtorParameterNamesShouldMatch : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0003";
        private const string Title = "Name the parameters to match the members.";
        private const string MessageFormat = "Name the parameters to match the members.";
        private const string Description = "Name the constructor parameters to match the properties or fields.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                                                                      id: DiagnosticId,
                                                                      title: Title,
                                                                      messageFormat: MessageFormat,
                                                                      category: AnalyzerCategory.Correctness,
                                                                      defaultSeverity: DiagnosticSeverity.Hidden,
                                                                      isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
                                                                      description: Description,
                                                                      helpLinkUri: HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleConstructor, SyntaxKind.ConstructorDeclaration);
        }

        private static void HandleConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructorDeclarationSyntax = (ConstructorDeclarationSyntax)context.Node;
            if (constructorDeclarationSyntax.ParameterList.Parameters.Count == 0)
            {
                return;
            }

            using (var pooled = ConstructorAssignmentsWalker.Create(constructorDeclarationSyntax, context.SemanticModel, context.CancellationToken))
            {
                foreach (var kvp in pooled.Item.ParameterNameMap)
                {
                    if (kvp.Value != null && !IsMatch(kvp.Key.Identifier, kvp.Value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, kvp.Key.Identifier.GetLocation()));
                    }
                }
            }
        }

        private static bool IsMatch(SyntaxToken identifier, string name)
        {
            if (identifier.ValueText == name)
            {
                return true;
            }

            return false;
        }
    }
}