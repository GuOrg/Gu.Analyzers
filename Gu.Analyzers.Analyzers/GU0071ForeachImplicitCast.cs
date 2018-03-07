namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0071ForeachImplicitCast : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0071";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Implicit casting done by the foreach",
            messageFormat: "Implicit cast done by the foreach",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "If an explicit type is used, the compiler inserts a cast. This was possibly useful in the pre-generic C# 1.0 era, but now it's a misfeature",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ForEachStatement);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ForEachStatementSyntax forEachStatement &&
               !forEachStatement.Type.IsVar &&
               EnumeratorTypeMatchesTheVariableType(context, forEachStatement) == false)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, forEachStatement.Type.GetLocation()));
            }
        }

        private static bool? EnumeratorTypeMatchesTheVariableType(SyntaxNodeAnalysisContext context, ForEachStatementSyntax forEachStatement)
        {
            var enumerableType = context.SemanticModel.GetTypeInfoSafe(forEachStatement.Expression, context.CancellationToken);
            var elementType = GetElementType(enumerableType.ConvertedType);
            if (elementType == null)
            {
                return null;
            }

            var variableType = context.SemanticModel.GetTypeInfoSafe(forEachStatement.Type, context.CancellationToken).Type;
            return SymbolComparer.Equals(variableType, elementType);
        }

        private static ITypeSymbol GetElementType(ITypeSymbol enumerableType)
        {
            if (enumerableType.TryFirstMethodRecursive("GetEnumerator", out var getEnumeratorMethod) &&
               getEnumeratorMethod.ReturnType is var enumeratorType &&
               enumeratorType.TryGetPropertyRecursive("Current", out var currentProperty) &&
               currentProperty.Type is var elementType)
            {
                return elementType;
            }

            return null;
        }
    }
}