namespace Gu.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0030UseUsing : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0030";
        private const string Title = "Use using.";
        private const string MessageFormat = "Use using.";
        private const string Description = "Use using.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.Correctness,
            DiagnosticSeverity.Warning,
            AnalyzerConstants.EnabledByDefault,
            Description,
            HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.VariableDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            var variableDeclaration = (VariableDeclarationSyntax)context.Node;
            VariableDeclaratorSyntax declarator;
            if (!variableDeclaration.Variables.TryGetSingle(out declarator))
            {
                return;
            }

            var symbol = context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken) as ILocalSymbol;
            if (symbol == null)
            {
                return;
            }

            if (Disposable.Is(symbol.Type))
            {
                if (Disposable.IsCreation(declarator.Initializer.Value, context.SemanticModel, context.CancellationToken) &&
                    !(variableDeclaration.Parent is UsingStatementSyntax))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, variableDeclaration.GetLocation()));
                }
            }
        }

        private static class Disposable
        {
            internal static bool IsCreation(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (expression is ObjectCreationExpressionSyntax)
                {
                    return true;
                }

                var symbol = semanticModel.SemanticModelFor(expression)
                                          .GetSymbolInfo(expression, cancellationToken)
                                          .Symbol;
                if (symbol is IFieldSymbol)
                {
                    return false;
                }

                if (symbol is IMethodSymbol)
                {
                    SyntaxReference syntaxReference;
                    if (symbol.DeclaringSyntaxReferences.TryGetSingle(out syntaxReference))
                    {
                        var methodDeclaration = (MethodDeclarationSyntax)syntaxReference.GetSyntax(cancellationToken);
                        ReturnStatementSyntax returnStatement;
                        if (TryGetReturnStatement(methodDeclaration, out returnStatement))
                        {
                            return IsCreation(returnStatement.Expression, semanticModel, cancellationToken);
                        }
                    }

                    return true;
                }

                var property = symbol as IPropertySymbol;
                if (property != null)
                {
                    if (property == KnownSymbol.PasswordBox.SecurePassword)
                    {
                        return true;
                    }

                    return false;
                }
                return false;
            }

            internal static bool Is(ITypeSymbol type)
            {
                if (type == null)
                {
                    return false;
                }

                ITypeSymbol _;
                return type == KnownSymbol.IDisposable ||
                       type.AllInterfaces.TryGetSingle(x => x == KnownSymbol.IDisposable, out _);
            }

            private static bool TryGetReturnStatement(MethodDeclarationSyntax method, out ReturnStatementSyntax result)
            {
                result = null;
                if (method.Body != null)
                {
                    foreach (var statementSyntax in method.Body.Statements)
                    {
                        var temp = statementSyntax as ReturnStatementSyntax;
                        if (result != null && temp != null)
                        {
                            return false;
                        }

                        result = temp;
                    }

                    return result != null;
                }

                if (method.ExpressionBody != null)
                {
                    throw new NotImplementedException();
                }

                return false;
            }
        }
    }
}