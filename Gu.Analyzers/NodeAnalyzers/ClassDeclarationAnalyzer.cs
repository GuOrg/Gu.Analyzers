namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ClassDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GU0024SealTypeWithDefaultMember.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.ClassDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.ContainingSymbol is INamedTypeSymbol type &&
                !type.IsStatic &&
                !type.IsSealed &&
                context.Node is ClassDeclarationSyntax classDeclaration)
            {
                foreach (var member in classDeclaration.Members)
                {
                    if (member is PropertyDeclarationSyntax property &&
                        IsStaticPublicOrInternal(property.Modifiers) &&
                        property.IsGetOnly() &&
                        property.Initializer?.Value is ObjectCreationExpressionSyntax objectCreation &&
                        context.SemanticModel.TryGetType(objectCreation, context.CancellationToken, out var createdType) &&
                        type.Equals(createdType))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GU0024SealTypeWithDefaultMember.Descriptor, classDeclaration.Identifier.GetLocation()));
                        return;
                    }
                }
            }
        }

        private static bool IsStaticPublicOrInternal(SyntaxTokenList propertyModifiers)
        {
            return propertyModifiers.Any(SyntaxKind.StaticKeyword) &&
                   !propertyModifiers.Any(SyntaxKind.ProtectedKeyword) &&
                   !propertyModifiers.Any(SyntaxKind.PrivateKeyword);
        }
    }
}
