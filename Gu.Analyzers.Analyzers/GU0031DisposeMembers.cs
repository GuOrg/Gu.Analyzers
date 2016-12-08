namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0031DisposeMembers : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0031";
        private const string Title = "Dispose members.";
        private const string MessageFormat = "Dispose members.";
        private const string Description = "Dispose members that are assigned with created `IDisposable`s anywhere within the class.";
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
                            var expressionSyntax = DisposedMember(disposeCall);
                            var disposedSymbol = context.SemanticModel.GetSymbolInfo(expressionSyntax, context.CancellationToken).Symbol;
                            if (ReferenceEquals(disposedSymbol, context.ContainingSymbol))
                            {
                                return;
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

        private static ExpressionSyntax DisposedMember(InvocationExpressionSyntax disposeCall)
        {
            var memberAccess = disposeCall.Expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                return memberAccess.Expression;
            }

            if (disposeCall.Expression is MemberBindingExpressionSyntax)
            {
                var conditionalAccess = (ConditionalAccessExpressionSyntax)disposeCall.Parent;
                return conditionalAccess.Expression;
            }

            throw new ArgumentOutOfRangeException(nameof(disposeCall), disposeCall, "Could not find disposed member.");
        }
    }
}