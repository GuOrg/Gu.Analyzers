namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0030UseUsing : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0030";
        private const string Title = "Use using.";
        private const string MessageFormat = "Use using.";
        private const string Description = "Use using.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.Correctness,
            DiagnosticSeverity.Warning,
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
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.VariableDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            var variableDeclaration = (VariableDeclarationSyntax)context.Node;
            VariableDeclaratorSyntax declarator;
            if (!variableDeclaration.Variables.TryGetSingle(out declarator))
            {
                return;
            }

            var symbol = context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken) as ILocalSymbol;
            ITypeSymbol _;
            if (symbol?.Type.AllInterfaces.TryGetSingle(x => x == KnownSymbol.IDisposable, out _) == true)
            {
                if (!(variableDeclaration.Parent is UsingStatementSyntax))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, variableDeclaration.GetLocation()));
                }
            }
        }
    }
}