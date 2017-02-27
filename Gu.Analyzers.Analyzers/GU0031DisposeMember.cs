namespace Gu.Analyzers
{
    using System.Collections.Immutable;

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
        private const string Description = "Dispose the member as it is assigned with a created `IDisposable`s within the type.";
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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

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
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var field = (IFieldSymbol)context.ContainingSymbol;
            if (field.IsStatic)
            {
                return;
            }

            if (Disposable.IsAssignedWithCreated(field, context.SemanticModel, context.CancellationToken)
                          .IsEither(Result.Yes, Result.Maybe))
            {
                if (!Disposable.IsMemberDisposed(field, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
        }

        private static void HandleProperty(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

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

            if (Disposable.IsAssignedWithCreatedAndNotCachedOrInjected(property, context.SemanticModel, context.CancellationToken))
            {
                if (!Disposable.IsMemberDisposed(property, context.SemanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
        }
    }
}