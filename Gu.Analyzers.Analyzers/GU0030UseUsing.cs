namespace Gu.Analyzers
{
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

            if (Disposable.IsAssignableTo(symbol.Type))
            {
                if (Disposable.IsCreation(declarator.Initializer.Value, context.SemanticModel, context.CancellationToken))
                {
                    if (variableDeclaration.Parent is UsingStatementSyntax)
                    {
                        return;
                    }

                    SyntaxNode declaration;
                    if (context.ContainingSymbol.TryGetDeclaration(context.CancellationToken, out declaration))
                    {
                        var methodDeclarationSyntax = declaration as MethodDeclarationSyntax;
                        ExpressionSyntax returnValue;
                        if (methodDeclarationSyntax.TryGetReturnExpression(out returnValue))
                        {
                            if ((returnValue as IdentifierNameSyntax)?.Identifier.ValueText == declarator.Identifier.ValueText)
                            {
                                return;
                            }
                        }

                        var getter = declaration as AccessorDeclarationSyntax;
                        if (getter?.Body?.TryGetReturnExpression(out returnValue) == true)
                        {
                            if ((returnValue as IdentifierNameSyntax)?.Identifier.ValueText == declarator.Identifier.ValueText)
                            {
                                return;
                            }
                        }
                    }

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
                    MethodDeclarationSyntax methodDeclaration;
                    if (symbol.TryGetDeclaration(cancellationToken, out methodDeclaration))
                    {
                        ExpressionSyntax returnValue;
                        if (methodDeclaration.TryGetReturnExpression(out returnValue))
                        {
                            return IsCreation(returnValue, semanticModel, cancellationToken);
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

                    PropertyDeclarationSyntax propertyDeclaration;
                    if (property.TryGetDeclaration(cancellationToken, out propertyDeclaration))
                    {
                        if (propertyDeclaration.ExpressionBody != null)
                        {
                            return IsCreation(propertyDeclaration.ExpressionBody.Expression, semanticModel, cancellationToken);
                        }

                        AccessorDeclarationSyntax getter;
                        if (propertyDeclaration.TryGetGetAccessorDeclaration(out getter))
                        {
                            ExpressionSyntax returnValue;
                            if (getter.Body.TryGetReturnExpression(out returnValue))
                            {
                                return IsCreation(returnValue, semanticModel, cancellationToken);
                            }
                        }
                    }

                    return false;
                }

                var local = symbol as ILocalSymbol;
                if (local != null)
                {
                    VariableDeclaratorSyntax variable;
                    if (local.TryGetDeclaration(cancellationToken, out variable))
                    {
                        return IsCreation(variable.Initializer.Value, semanticModel, cancellationToken);
                    }
                }

                return false;
            }

            internal static bool IsAssignableTo(ITypeSymbol type)
            {
                if (type == null)
                {
                    return false;
                }

                ITypeSymbol _;
                return type == KnownSymbol.IDisposable ||
                       type.AllInterfaces.TryGetSingle(x => x == KnownSymbol.IDisposable, out _);
            }
        }
    }
}