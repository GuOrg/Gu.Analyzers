namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0011DoNotIgnoreReturnValue : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Descriptor = Descriptors.Create(
            id: "GU0011",
            title: "Don't ignore the return value.",
            messageFormat: "Don't ignore the return value.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Don't ignore the return value.");

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => HandleCreation(c), SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(c => HandleInvocation(c), SyntaxKind.InvocationExpression);
        }

        private static void HandleCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                IsIgnored(objectCreation))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is InvocationExpressionSyntax invocation &&
                IsIgnored(invocation) &&
                !CanIgnore(invocation, context))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static bool IsIgnored(SyntaxNode node)
        {
            return node.Parent is ExpressionStatementSyntax expressionStatement &&
                   expressionStatement.Parent is BlockSyntax;
        }

        private static bool CanIgnore(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context)
        {
            if (context.SemanticModel.TryGetSymbol(invocation, context.CancellationToken, out var method))
            {
                if (method.ReturnsVoid ||
                    method.ContainingType.IsAssignableTo(KnownSymbol.MoqMockOfT, context.Compilation) ||
                    method.ContainingType.IsAssignableTo(KnownSymbol.MoqIFluentInterface, context.Compilation) ||
                    method.ContainingType.IsAssignableTo(KnownSymbol.NinjectIFluentSyntax, context.Compilation))
                {
                    return true;
                }

                if (ReferenceEquals(method.ContainingType, method.ReturnType) &&
                    method.ContainingType == KnownSymbol.StringBuilder)
                {
                    return true;
                }

                if ((method.Name == "Add" ||
                     method.Name == "Remove" ||
                     method.Name == "RemoveAll" ||
                     method.Name == "TryAdd" ||
                     method.Name == "TryRemove") &&
                    method.ReturnType.IsEither(KnownSymbol.Boolean, KnownSymbol.Int32, KnownSymbol.Int64))
                {
                    return true;
                }

                if (method.IsExtensionMethod)
                {
                    method = method.ReducedFrom;
                }

                if (method.TrySingleDeclaration(context.CancellationToken, out MethodDeclarationSyntax declaration) &&
                    ReturnValueWalker.TrySingle(declaration, out var returnValue))
                {
                    if (returnValue is IdentifierNameSyntax identifierName &&
                        method.Parameters.TryFirst(x => x.Name == identifierName.Identifier.ValueText, out _))
                    {
                        return true;
                    }

                    if (returnValue is InstanceExpressionSyntax)
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }
    }
}
