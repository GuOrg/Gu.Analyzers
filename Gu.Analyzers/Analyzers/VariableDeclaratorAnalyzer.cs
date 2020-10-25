namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class VariableDeclaratorAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.GU0018NameMock);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.VariableDeclarator);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is VariableDeclaratorSyntax variable &&
                context.SemanticModel.TryGetSymbol(variable, context.CancellationToken, out var symbol) &&
                Type(symbol) is { } type &&
                type is { IsGenericType: true, TypeArguments: { Length: 1 } typeArguments } &&
                typeArguments[0] is INamedTypeSymbol typeArgument &&
                type == KnownSymbol.MoqMockOfT &&
                ShouldRename(variable, typeArgument) is { } renameTo)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.GU0018NameMock,
                        variable.Identifier.GetLocation(),
                        ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>("Name", renameTo) })));
            }

            static INamedTypeSymbol? Type(ISymbol symbol)
            {
                return symbol switch
                {
                    ILocalSymbol local => local.Type as INamedTypeSymbol,
                    IFieldSymbol field => field.Type as INamedTypeSymbol,
                    _ => null,
                };
            }

            static string? ShouldRename(VariableDeclaratorSyntax current, INamedTypeSymbol type)
            {
                var expectedName = type switch
                {
                    { TypeKind: TypeKind.Interface, IsGenericType: false }
                        when type.Name.StartsWith("I", StringComparison.InvariantCulture) => $"{Prefix()}{type.Name.Substring(1).ToFirstCharLower()}Mock",
                    _ => null,
                };

                if (expectedName is null)
                {
                    return null;
                }

                if (current.Identifier.ValueText.IsParts("_", expectedName) ||
                    current.Identifier.ValueText == expectedName)
                {
                    return null;
                }

                return expectedName;

                string Prefix() => current.Identifier.ValueText.StartsWith("_", StringComparison.InvariantCulture)
                    ? "_"
                    : string.Empty;
            }
        }
    }
}
