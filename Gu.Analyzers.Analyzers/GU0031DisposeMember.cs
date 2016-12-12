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
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: Description,
            helpLinkUri: HelpLink);

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

            using (var pooled = MemberAssignmentWalker.Create(field, context.SemanticModel, context.CancellationToken))
            {
                if (IsAnyADisposableCreation(pooled.Item.Assignments, context.SemanticModel, context.CancellationToken))
                {
                    CheckThatMemberIsDisposed(context);
                }

                foreach (var assignment in pooled.Item.Assignments)
                {
                    var setter = assignment.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
                    if (setter?.IsKind(SyntaxKind.SetAccessorDeclaration) == true)
                    {
                        var property = context.SemanticModel.GetDeclaredSymbol(setter.FirstAncestorOrSelf<PropertyDeclarationSyntax>());
                        using (var pooledPropertyWalker = MemberAssignmentWalker.Create(property, context.SemanticModel, context.CancellationToken))
                        {
                            if (IsAnyADisposableCreation(pooledPropertyWalker.Item.Assignments, context.SemanticModel, context.CancellationToken))
                            {
                                CheckThatMemberIsDisposed(context);
                            }
                        }
                    }
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

            using (var pooled = MemberAssignmentWalker.Create(property, context.SemanticModel, context.CancellationToken))
            {
                if (IsAnyADisposableCreation(pooled.Item.Assignments, context.SemanticModel, context.CancellationToken))
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
                foreach (var declaration in disposeMethod.Declarations(context.CancellationToken))
                {
                    using (var pooled = IdentifierNameWalker.Create(declaration))
                    {
                        foreach (var identifier in pooled.Item.IdentifierNames)
                        {
                            if (identifier.Identifier.ValueText != context.ContainingSymbol.Name)
                            {
                                continue;
                            }

                            var symbol = context.SemanticModel.SemanticModelFor(identifier)
                                                .GetSymbolInfo(identifier, context.CancellationToken)
                                                .Symbol;
                            if (ReferenceEquals(symbol, context.ContainingSymbol))
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
    }
}