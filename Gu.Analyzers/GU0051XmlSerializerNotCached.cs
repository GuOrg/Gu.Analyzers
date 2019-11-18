namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0051XmlSerializerNotCached : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0051XmlSerializerNotCached);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ObjectCreationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                context.SemanticModel.TryGetSymbol(objectCreation, KnownSymbol.XmlSerializer, context.CancellationToken, out var ctor) &&
                IsLeakyConstructor(ctor))
            {
                if (objectCreation.FirstAncestor<AssignmentExpressionSyntax>() is { Left: { } left } assignment)
                {
                    if (context.SemanticModel.GetSymbolSafe(left, context.CancellationToken) is { CanBeReferencedByName: true } assignmentSymbol &&
                        assignmentSymbol is IFieldSymbol { IsStatic: true, IsReadOnly: true })
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0051XmlSerializerNotCached, assignment.GetLocation()));
                    return;
                }

                if (objectCreation.FirstAncestor<VariableDeclaratorSyntax>() is { } declarator)
                {
                    if (context.SemanticModel.GetDeclaredSymbolSafe(declarator, context.CancellationToken) is IFieldSymbol { IsStatic: true, IsReadOnly: true })
                    {
                        return;
                    }
                }

                var variableDeclaration = objectCreation.FirstAncestor<VariableDeclarationSyntax>();
                if (variableDeclaration != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0051XmlSerializerNotCached, variableDeclaration.GetLocation()));
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0051XmlSerializerNotCached, objectCreation.GetLocation()));
            }
        }

        private static bool IsLeakyConstructor(IMethodSymbol ctor)
        {
            var parameters = ctor.Parameters;
            if (parameters.Length == 1 && parameters[0].Type == KnownSymbol.Type)
            {
                return false;
            }

            if (parameters.Length == 2 && parameters[0].Type == KnownSymbol.Type && parameters[1].Type == KnownSymbol.String)
            {
                return false;
            }

            return true;
        }
    }
}
