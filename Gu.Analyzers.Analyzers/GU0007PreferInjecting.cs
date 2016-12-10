namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0007PreferInjecting : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0007";
        private const string Title = "Prefer injecting.";
        private const string MessageFormat = "Prefer injecting.";
        private const string Description = "Prefer injecting.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.Correctness,
            DiagnosticSeverity.Hidden,
            AnalyzerConstants.EnabledByDefault,
            Description,
            HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ObjectCreationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            if (objectCreation.FirstAncestorOrSelf<ConstructorDeclarationSyntax>()?.Modifiers.Any(SyntaxKind.StaticKeyword) != false)
            {
                return;
            }

            var ctor = context.SemanticModel.GetSymbolInfo(objectCreation)
                                .Symbol as IMethodSymbol;
            if (ctor == null || IsValidCreationType(ctor))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, objectCreation.GetLocation()));
        }

        private static bool IsValidCreationType(IMethodSymbol ctor)
        {
            if (ctor.ContainingType.IsValueType ||
                ctor.IsStatic)
            {
                return true;
            }

            foreach (var namespaceSymbol in ctor.ContainingNamespace.ConstituentNamespaces)
            {
                if (namespaceSymbol.Name == "System")
                {
                    return true;
                }
            }

            return false;
        }
    }
}