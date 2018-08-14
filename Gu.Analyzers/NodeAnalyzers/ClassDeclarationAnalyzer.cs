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
                        IsInitializedWithContainingType(property.Initializer, context))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GU0024SealTypeWithDefaultMember.Descriptor, classDeclaration.Identifier.GetLocation()));
                        return;
                    }

                    if (member is FieldDeclarationSyntax field &&
                            IsStaticPublicOrInternal(field.Modifiers) &&
                            field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) &&
                            field.Declaration is VariableDeclarationSyntax declaration &&
                            declaration.Variables.TrySingle(out var variable) &&
                             IsInitializedWithContainingType(variable.Initializer, context))
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

        private static bool IsInitializedWithContainingType(EqualsValueClauseSyntax iniializer, SyntaxNodeAnalysisContext context)
        {
            return iniializer?.Value is ObjectCreationExpressionSyntax objectCreation &&
                   context.SemanticModel.TryGetType(objectCreation, context.CancellationToken, out var createdType) &&
                   createdType.Equals(context.ContainingSymbol);
        }
    }
}
