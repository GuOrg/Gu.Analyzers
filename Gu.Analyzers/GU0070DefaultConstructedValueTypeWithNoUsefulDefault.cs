namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0070DefaultConstructedValueTypeWithNoUsefulDefault : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0070";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Default-constructed value type with no useful default",
            messageFormat: "Default constructed value type was created, which is likely not what was intended.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Types declared with struct must have a default constructor, even if there is no semantically sensible default value for that type. Examples include System.Guid and System.DateTime.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        private static readonly List<QualifiedType> KnownTypes = new List<QualifiedType>
        {
            KnownSymbol.Guid,
            KnownSymbol.DateTime,
        };

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ObjectCreationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.ArgumentList != null &&
                objectCreation.ArgumentList.Arguments.Count == 0 &&
                IsTheCreatedTypeKnownForHavingNoUsefulDefault(context, objectCreation, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, objectCreation.GetLocation()));
            }
        }

        private static bool IsTheCreatedTypeKnownForHavingNoUsefulDefault(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation, out IMethodSymbol ctor)
        {
            // TODO: Stop using linear search if the number of types becomes large
            foreach (var qualifiedType in KnownTypes)
            {
                if (context.SemanticModel.TryGetSymbol(objectCreation, qualifiedType, context.CancellationToken, out ctor))
                {
                    return true;
                }
            }

            ctor = null;
            return false;
        }
    }
}
