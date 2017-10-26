namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Analyzers.Helpers;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU100TestMethodDoesNotUseMember : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0100";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Test can be parallelized.",
            messageFormat: "Test can be parallelized.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: AnalyzerConstants.DisabledByDefault,
            description: "The test method does not touch any field or property.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.Handle, SyntaxKind.MethodDeclaration);
        }

        private void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is MethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.IsTestMethod(context.SemanticModel, context.CancellationToken))
            {
                using (var walker = IdentifierNameWalker.Borrow(methodDeclaration))
                {
                    foreach (var name in walker.IdentifierNames)
                    {
                        var symbol = context.SemanticModel.GetSymbolSafe(name, context.CancellationToken);
                        if (symbol is IFieldSymbol field &&
                            ReferenceEquals(field.ContainingType, context.ContainingSymbol))
                        {
                            return;
                        }

                        if (symbol is IPropertySymbol property &&
                            ReferenceEquals(property.ContainingType, context.ContainingSymbol))
                        {
                            return;
                        }

                        if (symbol is IMethodSymbol method &&
                            ReferenceEquals(method.ContainingType, context.ContainingSymbol))
                        {
                            return;
                        }
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, methodDeclaration.GetLocation()));
            }
        }
    }
}