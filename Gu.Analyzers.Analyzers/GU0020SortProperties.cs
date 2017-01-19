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
    internal class GU0020SortProperties : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0020";
        private const string Title = "Sort properties.";
        private const string MessageFormat = "Move property.";
        private const string Description = "Sort properties by StyleCop rules then by mutability.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
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
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.IndexerDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var property = context.ContainingSymbol as IPropertySymbol;
            if (property == null)
            {
                return;
            }

            var basePropertyDeclaration = (BasePropertyDeclarationSyntax)context.Node;
            var neighbors = GetNeighbors(basePropertyDeclaration, context.SemanticModel, context.CancellationToken);
            if (neighbors.Before != null)
            {
                if (PropertyPositionComparer.Default.Compare(neighbors.Before, property) > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }

            if (neighbors.After != null)
            {
                if (PropertyPositionComparer.Default.Compare(neighbors.After, property) < 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
        }

        private static Neighbors GetNeighbors(BasePropertyDeclarationSyntax propertyDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var typeDeclaration = propertyDeclaration.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
            {
                return default(Neighbors);
            }

            bool isBefore = true;
            BasePropertyDeclarationSyntax before = null;
            BasePropertyDeclarationSyntax after = null;
            foreach (var member in typeDeclaration.Members)
            {
                var declaration = member as BasePropertyDeclarationSyntax;
                if (declaration == null ||
                    !declaration.IsPropertyOrIndexer())
                {
                    continue;
                }

                if (declaration == propertyDeclaration)
                {
                    isBefore = false;
                    continue;
                }

                if (isBefore)
                {
                    before = declaration;
                }
                else
                {
                    after = declaration;
                    break;
                }
            }

            return new Neighbors((IPropertySymbol)semanticModel.GetDeclaredSymbolSafe(before, cancellationToken), (IPropertySymbol)semanticModel.GetDeclaredSymbolSafe(after, cancellationToken));
        }

        private struct Neighbors
        {
#pragma warning disable RS1008 // Avoid storing per-compilation data into the fields of a diagnostic analyzer.
            internal readonly IPropertySymbol Before;
            internal readonly IPropertySymbol After;
#pragma warning restore RS1008 // Avoid storing per-compilation data into the fields of a diagnostic analyzer.

            public Neighbors(IPropertySymbol before, IPropertySymbol after)
            {
                this.Before = before;
                this.After = after;
            }
        }

        internal class PropertyPositionComparer : IComparer<IPropertySymbol>
        {
            public static readonly PropertyPositionComparer Default = new PropertyPositionComparer();

            public int Compare(IPropertySymbol x, IPropertySymbol y)
            {
                int result;
                if (TryCompare(x, y, p => !p.IsIndexer, out result) ||
                    TryCompare(x, y, p => p.DeclaredAccessibility == Accessibility.Public, out result) ||
                    TryCompare(x, y, p => IsExplicit(p, Accessibility.Public), out result) ||
                    TryCompare(x, y, p => p.DeclaredAccessibility == Accessibility.Internal, out result) ||
                    TryCompare(x, y, p => IsExplicit(p, Accessibility.Internal), out result) ||
                    TryCompare(x, y, p => p.DeclaredAccessibility == Accessibility.Protected, out result) ||
                    TryCompare(x, y, p => p.DeclaredAccessibility == Accessibility.Private, out result) ||
                    TryCompare(x, y, p => p.IsStatic, out result) ||
                    TryCompare(x, y, IsGetOnly, out result) ||
                    TryCompare(x, y, IsCalculated, out result) ||
                    TryCompare(x, y, p => p.SetMethod?.DeclaredAccessibility == Accessibility.Private, out result) ||
                    TryCompare(x, y, p => p.SetMethod?.DeclaredAccessibility == Accessibility.Protected, out result) ||
                    TryCompare(x, y, p => p.SetMethod?.DeclaredAccessibility == Accessibility.Internal, out result))
                {
                    return result;
                }

                return 0;
            }

            private static bool IsExplicit(IPropertySymbol property, Accessibility accessibility)
            {
                IPropertySymbol interfaceProperty;
                if (property.ExplicitInterfaceImplementations.TryGetSingle(out interfaceProperty))
                {
                    return interfaceProperty.DeclaredAccessibility == accessibility;
                }

                return false;
            }

            private static bool TryCompare<T>(T x, T y, Func<T, bool> criteria, out int result)
            {
                return TryCompare(criteria(x), criteria(y), out result);
            }

            private static bool TryCompare(bool x, bool y, out int result)
            {
                if (x == y)
                {
                    result = 0;
                    return false;
                }

                if (x)
                {
                    result = -1;
                    return true;
                }

                result = 1;
                return true;
            }

            private static bool IsGetOnly(IPropertySymbol property)
            {
                BasePropertyDeclarationSyntax declaration;
                if (!TryGetDeclaration(property, out declaration))
                {
                    return false;
                }

                AccessorDeclarationSyntax getter;
                if (!TryGetGetter(declaration, out getter) ||
                    getter.Body != null)
                {
                    return false;
                }

                AccessorDeclarationSyntax _;
                return !TryGetSetter(declaration, out _);
            }

            private static bool IsCalculated(IPropertySymbol property)
            {
                BasePropertyDeclarationSyntax declaration;
                if (!TryGetDeclaration(property, out declaration))
                {
                    return false;
                }

                AccessorDeclarationSyntax _;
                if (TryGetSetter(declaration, out _))
                {
                    return false;
                }

                AccessorDeclarationSyntax getter;
                if (TryGetGetter(declaration, out getter) && getter.Body != null)
                {
                    return true;
                }

                return (declaration as PropertyDeclarationSyntax)?.ExpressionBody != null;
            }

            private static bool TryGetGetter(BasePropertyDeclarationSyntax declaration, out AccessorDeclarationSyntax getter)
            {
                return declaration.TryGetGetAccessorDeclaration(out getter);
            }

            private static bool TryGetSetter(BasePropertyDeclarationSyntax declaration, out AccessorDeclarationSyntax setter)
            {
                return declaration.TryGetSetAccessorDeclaration(out setter);
            }

            private static bool TryGetDeclaration(IPropertySymbol property, out BasePropertyDeclarationSyntax declaration)
            {
                if (property.DeclaringSyntaxReferences.Length != 1)
                {
                    declaration = null;
                    return false;
                }

                declaration = (BasePropertyDeclarationSyntax)property.DeclaringSyntaxReferences[0].GetSyntax();
                return declaration != null;
            }
        }
    }
}