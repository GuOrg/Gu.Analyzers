namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0031DisposeMember : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0031";
        private const string Title = "Dispose member.";
        private const string MessageFormat = "Dispose member.";
        private const string Description = "Dispose member that is assigned with created `IDisposable`s anywhere within the type.";
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
            context.RegisterSyntaxNodeAction(HandleField, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(HandleProperty, SyntaxKind.PropertyDeclaration);
        }

        private static void HandleField(SyntaxNodeAnalysisContext context)
        {
            var field = (IFieldSymbol)context.ContainingSymbol;
            if (field.IsStatic)
            {
                return;
            }

            using (var walker = AssignmentWalker.Create(field, context.SemanticModel, context.CancellationToken))
            {
                if (IsAnyADisposableCreation(walker.Assignments, context.SemanticModel, context.CancellationToken))
                {
                    CheckThatMemberIsDisposed(context);
                }
            }
        }

        private static void HandleProperty(SyntaxNodeAnalysisContext context)
        {
            var property = (IPropertySymbol)context.ContainingSymbol;
            if (property.IsStatic ||
                property.IsIndexer)
            {
                return;
            }

            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
            if (propertyDeclaration.ExpressionBody != null)
            {
                return;
            }

            AccessorDeclarationSyntax setter;
            if (propertyDeclaration.TryGetSetAccessorDeclaration(out setter) &&
                setter.Body != null)
            {
                // Handle the backing field
                return;
            }

            using (var walker = AssignmentWalker.Create(property, context.SemanticModel, context.CancellationToken))
            {
                if (IsAnyADisposableCreation(walker.Assignments, context.SemanticModel, context.CancellationToken))
                {
                    CheckThatMemberIsDisposed(context);
                }
            }
        }

        private static void CheckThatMemberIsDisposed(SyntaxNodeAnalysisContext context)
        {
            var containingType = context.ContainingSymbol.ContainingType;
            if (!Disposable.IsAssignableTo(containingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                return;
            }

            foreach (var disposeMethod in containingType.GetMembers("Dispose").OfType<IMethodSymbol>())
            {
                MethodDeclarationSyntax declaration;
                if (disposeMethod.TryGetSingleDeclaration(context.CancellationToken, out declaration))
                {
                    using (var walker = Disposable.CreateDisposeWalker(declaration.Body, context.SemanticModel, context.CancellationToken))
                    {
                        foreach (var disposeCall in walker.DisposeCalls)
                        {
                            ISymbol disposedSymbol;
                            if (TryFindDisposedMember(disposeCall, context.SemanticModel, context.CancellationToken, out disposedSymbol))
                            {
                                if (ReferenceEquals(disposedSymbol, context.ContainingSymbol))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
        }

        private static bool IsAnyADisposableCreation(IReadOnlyList<ExpressionSyntax> assignments, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var assignment in assignments)
            {
                if (Disposable.IsCreation(assignment, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindDisposedMember(
            InvocationExpressionSyntax disposeCall,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ISymbol disposedMember)
        {
            disposedMember = null;
            ExpressionSyntax invokee;
            if (disposeCall.TryFindInvokee(out invokee))
            {
                var symbol = semanticModel.GetSymbolInfo(invokee, cancellationToken).Symbol;
                if (symbol is IPropertySymbol ||
                    symbol is IFieldSymbol)
                {
                    disposedMember = symbol;
                    return true;
                }

                var localSymbol = symbol as ILocalSymbol;
                VariableDeclaratorSyntax declarator;
                if (localSymbol.TryGetSingleDeclaration(cancellationToken, out declarator))
                {
                    var initializerValue = declarator.Initializer?.Value;
                    if (initializerValue == null)
                    {
                        return false;
                    }

                    if (initializerValue is IdentifierNameSyntax)
                    {
                        disposedMember = semanticModel.GetSymbolInfo(initializerValue, cancellationToken)
                                                      .Symbol;
                    }
                    else if (initializerValue.IsKind(SyntaxKind.AsExpression))
                    {
                        disposedMember = semanticModel.GetSymbolInfo(((BinaryExpressionSyntax)initializerValue).Left, cancellationToken)
                                                      .Symbol;
                    }
                    else if (initializerValue is CastExpressionSyntax)
                    {
                        disposedMember = semanticModel.GetSymbolInfo(((CastExpressionSyntax)initializerValue).Expression, cancellationToken)
                              .Symbol;
                    }

                    if (disposedMember is IPropertySymbol || disposedMember is IFieldSymbol)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}