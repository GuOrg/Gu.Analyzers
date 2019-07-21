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
                var assignment = objectCreation.FirstAncestor<AssignmentExpressionSyntax>();
                if (assignment != null)
                {
                    var assignmentSymbol = context.SemanticModel.GetSymbolSafe(
                        assignment.Left,
                        context.CancellationToken);
                    if (assignmentSymbol != null &&
                        assignmentSymbol.CanBeReferencedByName &&
                        assignmentSymbol.DeclaringSyntaxReferences.TrySingle(out var assignedToDeclaration))
                    {
                        var assignedToDeclarator =
                            assignedToDeclaration.GetSyntax(context.CancellationToken) as VariableDeclaratorSyntax;
                        if (context.SemanticModel.SemanticModelFor(assignedToDeclarator)
                                   ?.GetDeclaredSymbol(
                                       assignedToDeclarator,
                                       context.CancellationToken) is IFieldSymbol fieldSymbol &&
                            fieldSymbol.IsReadOnly &&
                            fieldSymbol.IsStatic)
                        {
                            return;
                        }
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0051XmlSerializerNotCached, assignment.GetLocation()));
                    return;
                }

                var declarator = objectCreation.FirstAncestor<VariableDeclaratorSyntax>();
                if (declarator != null)
                {
                    if (context.SemanticModel.SemanticModelFor(declarator)
                               ?.GetDeclaredSymbol(declarator, context.CancellationToken) is IFieldSymbol
                            fieldSymbol &&
                        fieldSymbol.IsReadOnly &&
                        fieldSymbol.IsStatic)
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
