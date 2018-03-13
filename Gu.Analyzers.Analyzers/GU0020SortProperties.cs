namespace Gu.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

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

            if (context.Node is BasePropertyDeclarationSyntax basePropertyDeclarationSyntax)
            {
                var neighbors = GetNeighbors(basePropertyDeclarationSyntax);
                if (neighbors.Before != null)
                {
                    if (PropertyPositionComparer.Default.Compare(neighbors.Before, basePropertyDeclarationSyntax) > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                    }
                }

                if (neighbors.After != null)
                {
                    if (PropertyPositionComparer.Default.Compare(neighbors.After, basePropertyDeclarationSyntax) < 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                    }
                }
            }
        }

        private static Neighbors GetNeighbors(BasePropertyDeclarationSyntax propertyDeclaration)
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

            return new Neighbors(before, after);
        }

        private struct Neighbors
        {
#pragma warning disable RS1008 // Avoid storing per-compilation data into the fields of a diagnostic analyzer.
            internal readonly BasePropertyDeclarationSyntax Before;
            internal readonly BasePropertyDeclarationSyntax After;
#pragma warning restore RS1008 // Avoid storing per-compilation data into the fields of a diagnostic analyzer.

            public Neighbors(BasePropertyDeclarationSyntax before, BasePropertyDeclarationSyntax after)
            {
                this.Before = before;
                this.After = after;
            }
        }

        internal class PropertyPositionComparer : IComparer<BasePropertyDeclarationSyntax>
        {
            public static readonly PropertyPositionComparer Default = new PropertyPositionComparer();

            private static readonly Accessibility[] AccessibilityOrder =
            {
                Accessibility.Public,
                Accessibility.ProtectedAndInternal,
                Accessibility.Internal,
                Accessibility.Protected,
                Accessibility.Private
            };

            public int Compare(BasePropertyDeclarationSyntax x, BasePropertyDeclarationSyntax y)
            {
                if (IsInitializedWithOther(x, y, out var result) ||
                    TryCompare(x, y, IsNotIndexer, out result) ||
                    TryCompareAccessibility(x, y, out result) ||
                    TryCompare(x, y, IsStatic, out result) ||
                    TryCompare(x, y, IsGetOnly, out result) ||
                    TryCompare(x, y, IsCalculated, out result) ||
                    TryCompareSetMethodDeclaredAccessibility(x, y, out result))
                {
                    return result;
                }

                return 0;
            }

            private static bool IsInitializedWithOther(BasePropertyDeclarationSyntax x, BasePropertyDeclarationSyntax y, out int result)
            {
                bool IsInitializedWith(EqualsValueClauseSyntax initializer, PropertyDeclarationSyntax other)
                {
                    using (var identifiers = IdentifierNameWalker.Borrow(initializer))
                    {
                        foreach (var identifier in identifiers.IdentifierNames)
                        {
                            if (identifier.Identifier.ValueText == other.Identifier.ValueText)
                            {
                                if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
                                    memberAccess.Expression is IdentifierNameSyntax typeIdentifier &&
                                    other.Parent is TypeDeclarationSyntax typeDeclarationSyntax &&
                                    typeIdentifier.Identifier.ValueText != typeDeclarationSyntax.Identifier.ValueText)
                                {
                                    continue;
                                }

                                return true;
                            }
                        }
                    }

                    return false;
                }

                if (x is PropertyDeclarationSyntax xDeclaration &&
                    y is PropertyDeclarationSyntax yDeclaration &&
                    xDeclaration.Parent == yDeclaration.Parent)
                {
                    if (xDeclaration.Initializer is EqualsValueClauseSyntax xi &&
                        IsInitializedWith(xi, yDeclaration))
                    {
                        result = 1;
                        return true;
                    }

                    if (yDeclaration.Initializer is EqualsValueClauseSyntax yi &&
                        IsInitializedWith(yi, xDeclaration))
                    {
                        result = -1;
                        return true;
                    }
                }

                result = 0;
                return false;
            }

            private static bool TryCompareAccessibility(BasePropertyDeclarationSyntax x, BasePropertyDeclarationSyntax y, out int result)
            {
                var xa = x.GetAccessibility();
                var ya = y.GetAccessibility();
                if (xa == ya)
                {
                    result = 0;
                    return false;
                }

                result = Array.IndexOf(AccessibilityOrder, xa)
                              .CompareTo(Array.IndexOf(AccessibilityOrder, ya));
                return true;
            }

            private static bool TryCompareSetMethodDeclaredAccessibility(BasePropertyDeclarationSyntax x, BasePropertyDeclarationSyntax y, out int result)
            {
                if (TryGetSetter(x, out AccessorDeclarationSyntax xs) &&
                    TryGetSetter(y, out AccessorDeclarationSyntax ys))
                {
                    var xa = GetAccessibility(xs.Modifiers, x.GetAccessibility());
                    var ya = GetAccessibility(ys.Modifiers, y.GetAccessibility());
                    if (xa == ya)
                    {
                        result = 0;
                        return false;
                    }

                    result = -1 * Array.IndexOf(AccessibilityOrder, xa)
                                       .CompareTo(Array.IndexOf(AccessibilityOrder, ya));
                    return true;
                }

                result = 0;
                return false;
            }

            private static Accessibility GetAccessibility(SyntaxTokenList modifiers, Accessibility @default)
            {
                if (modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    return Accessibility.Private;
                }

                if (modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    return Accessibility.Public;
                }

                if (modifiers.Any(SyntaxKind.ProtectedKeyword) &&
                    modifiers.Any(SyntaxKind.InternalKeyword))
                {
                    return Accessibility.ProtectedAndInternal;
                }

                if (modifiers.Any(SyntaxKind.InternalKeyword))
                {
                    return Accessibility.Internal;
                }

                if (modifiers.Any(SyntaxKind.ProtectedKeyword))
                {
                    return Accessibility.Protected;
                }

                return @default;
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

            private static bool IsNotIndexer(BasePropertyDeclarationSyntax property)
            {
                return !(property is IndexerDeclarationSyntax);
            }

            private static bool IsStatic(BasePropertyDeclarationSyntax property)
            {
                return property.Modifiers.Any(SyntaxKind.StaticKeyword);
            }

            private static bool IsGetOnly(BasePropertyDeclarationSyntax property)
            {
                if (!TryGetGetter(property, out var getter) ||
                    getter.Body != null)
                {
                    return false;
                }

                return !TryGetSetter(property, out AccessorDeclarationSyntax _);
            }

            private static bool IsCalculated(BasePropertyDeclarationSyntax property)
            {
                if (!(property is PropertyDeclarationSyntax || property is IndexerDeclarationSyntax))
                {
                    return false;
                }

                if (TryGetSetter(property, out AccessorDeclarationSyntax _))
                {
                    return false;
                }

                if (TryGetGetter(property, out var getter) &&
                    getter.Body != null)
                {
                    return true;
                }

                return (property as PropertyDeclarationSyntax)?.ExpressionBody != null ||
                       (property as IndexerDeclarationSyntax)?.ExpressionBody != null;
            }

            private static bool TryGetGetter(BasePropertyDeclarationSyntax declaration, out AccessorDeclarationSyntax getter)
            {
                return declaration.TryGetGetter(out getter);
            }

            private static bool TryGetSetter(BasePropertyDeclarationSyntax declaration, out AccessorDeclarationSyntax setter)
            {
                return declaration.TryGetSetter(out setter);
            }
        }
    }
}