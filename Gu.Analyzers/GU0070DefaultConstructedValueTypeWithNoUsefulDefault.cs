namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0070DefaultConstructedValueTypeWithNoUsefulDefault : DiagnosticAnalyzer
    {
        private static readonly List<QualifiedType> KnownTypes = new List<QualifiedType>
        {
            KnownSymbol.Guid,
            KnownSymbol.DateTime,
        };

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptors.GU0070DefaultConstructedValueTypeWithNoUsefulDefault);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
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
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0070DefaultConstructedValueTypeWithNoUsefulDefault, objectCreation.GetLocation()));
            }
        }

        private static bool IsTheCreatedTypeKnownForHavingNoUsefulDefault(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation, [NotNullWhen(true)] out IMethodSymbol? ctor)
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
