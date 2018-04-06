namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0052ExceptionShouldBeSerializable : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0052";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Mark exception with [Serializable].",
            messageFormat: "Mark exception with [Serializable].",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Mark exception with [Serializable].",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ClassDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ClassDeclarationSyntax classDeclaration &&
                context.ContainingSymbol is INamedTypeSymbol type &&
                IsOfType(type, "Exception"))
            {
                System.Diagnostics.Debug.WriteLine($"has SerilizableAttribute:{type.GetAttributes().TryFirst(attr => attr.AttributeClass == KnownSymbol.SerializableAttribute, out _)}");
                if (!type.GetAttributes().Any(attr => attr.AttributeClass == KnownSymbol.SerializableAttribute))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, classDeclaration.Identifier.GetLocation()));
                }
            }
        }

        private static bool IsOfType(ITypeSymbol type, string typeName)
        {
            if (type == null)
            {
                return false;
            }

            if (type.Name == typeName)
            {
                return true;
            }

            if (type.Name == "Object")
            {
                return false;
            }

            return IsOfType(type.BaseType, typeName);
        }
    }
}
