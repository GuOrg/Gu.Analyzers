namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0051XmlSerializerNotCached : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0051";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Cache the XmlSerializer.",
            messageFormat: "The serializer is not cached.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "This constructor loads assemblies in non-GC memory, which may cause memory leaks.",
            helpLinkUri: Analyzers.HelpLink.ForId(DiagnosticId));

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
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                TryGetConstructor(objectCreation, context, out var ctor) &&
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
                        assignmentSymbol.DeclaringSyntaxReferences.TryGetSingle(out var assignedToDeclaration))
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

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation()));
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
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, variableDeclaration.GetLocation()));
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, objectCreation.GetLocation()));
            }
        }

        private static bool TryGetConstructor(ObjectCreationExpressionSyntax objectCreation, SyntaxNodeAnalysisContext context, out IMethodSymbol ctor)
        {
            if (objectCreation.Type is IdentifierNameSyntax typeName &&
                typeName.Identifier.ValueText == KnownSymbol.XmlSerializer.Type)
            {
                ctor = context.SemanticModel.GetSymbolSafe(objectCreation, context.CancellationToken) as IMethodSymbol;
                return ctor?.ContainingType == KnownSymbol.XmlSerializer;
            }

            ctor = null;
            return false;
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