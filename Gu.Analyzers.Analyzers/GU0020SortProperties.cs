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

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Sort properties.",
            messageFormat: "Move property.",
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Sort properties by StyleCop rules then by mutability.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

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

            var isBefore = true;
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
                if (TryCompare(x, y, p => !p.IsIndexer, out int result) ||
    TryCompare(x, y, p => IsDeclaredAccessibility(p, Accessibility.Public), out result) ||
    TryCompare(x, y, p => IsDeclaredAccessibility(p, Accessibility.Friend), out result) ||
    TryCompare(x, y, p => IsDeclaredAccessibility(p, Accessibility.ProtectedAndFriend), out result) ||
    TryCompare(x, y, p => IsDeclaredAccessibility(p, Accessibility.Internal), out result) ||
    TryCompare(x, y, p => IsDeclaredAccessibility(p, Accessibility.ProtectedAndInternal), out result) ||
    TryCompare(x, y, p => IsDeclaredAccessibility(p, Accessibility.Protected), out result) ||
    TryCompare(x, y, p => IsDeclaredAccessibility(p, Accessibility.Private), out result) ||
    TryCompare(x, y, p => p.IsStatic, out result) ||
    TryCompare(x, y, IsGetOnly, out result) ||
    TryCompare(x, y, IsCalculated, out result) ||
    TryCompare(x, y, p => IsSetMethodDeclaredAccessibility(p, Accessibility.Private), out result) ||
    TryCompare(x, y, p => IsSetMethodDeclaredAccessibility(p, Accessibility.Protected), out result) ||
    TryCompare(x, y, p => IsSetMethodDeclaredAccessibility(p, Accessibility.ProtectedAndInternal), out result) ||
    TryCompare(x, y, p => IsSetMethodDeclaredAccessibility(p, Accessibility.Friend), out result) ||
    TryCompare(x, y, p => IsSetMethodDeclaredAccessibility(p, Accessibility.Internal), out result))
                {
                    return result;
                }

                return 0;
            }

            private static bool IsDeclaredAccessibility(IPropertySymbol property, Accessibility accessibility)
            {
                if (property.ExplicitInterfaceImplementations.TryGetSingle(out IPropertySymbol interfaceProperty))
                {
                    return interfaceProperty.DeclaredAccessibility == accessibility;
                }

                return property.DeclaredAccessibility == accessibility;
            }

            private static bool IsSetMethodDeclaredAccessibility(IPropertySymbol property, Accessibility accessibility)
            {
                if (property.ExplicitInterfaceImplementations.TryGetSingle(out IPropertySymbol interfaceProperty))
                {
                    return interfaceProperty.DeclaredAccessibility == accessibility;
                }

                return property.SetMethod?.DeclaredAccessibility == accessibility;
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
                if (!TryGetDeclaration(property, out BasePropertyDeclarationSyntax declaration))
                {
                    return false;
                }

                if (!TryGetGetter(declaration, out AccessorDeclarationSyntax getter) ||
    getter.Body != null)
                {
                    return false;
                }

                return !TryGetSetter(declaration, out AccessorDeclarationSyntax _);
            }

            private static bool IsCalculated(IPropertySymbol property)
            {
                if (!TryGetDeclaration(property, out BasePropertyDeclarationSyntax declaration))
                {
                    return false;
                }

                if (!(declaration is PropertyDeclarationSyntax || declaration is IndexerDeclarationSyntax))
                {
                    return false;
                }

                if (TryGetSetter(declaration, out AccessorDeclarationSyntax _))
                {
                    return false;
                }

                if (TryGetGetter(declaration, out AccessorDeclarationSyntax getter) && getter.Body != null)
                {
                    return true;
                }

                return (declaration as PropertyDeclarationSyntax)?.ExpressionBody != null ||
                       (declaration as IndexerDeclarationSyntax)?.ExpressionBody != null;
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