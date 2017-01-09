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

            var property = (BasePropertyDeclarationSyntax)context.Node;
            var neighbors = GetNeighbors(property);
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

        private static Neighbors GetNeighbors(BasePropertyDeclarationSyntax propertyDeclaration)
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
                if (declaration == null)
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
            internal readonly BasePropertyDeclarationSyntax Before;
            internal readonly BasePropertyDeclarationSyntax After;

            public Neighbors(BasePropertyDeclarationSyntax before, BasePropertyDeclarationSyntax after)
            {
                this.Before = before;
                this.After = after;
            }
        }

        internal class PropertyPositionComparer : IComparer<BasePropertyDeclarationSyntax>
        {
            public static readonly PropertyPositionComparer Default = new PropertyPositionComparer();

            public int Compare(BasePropertyDeclarationSyntax x, BasePropertyDeclarationSyntax y)
            {
                AccessorDeclarationSyntax xSetter;
                if (!x.TryGetSetAccessorDeclaration(out xSetter))
                {
                    xSetter = null;
                }

                AccessorDeclarationSyntax ySetter;
                if (!y.TryGetSetAccessorDeclaration(out ySetter))
                {
                    ySetter = null;
                }

                int result;
                if (TryCompare(x, y, p => p.Modifiers.Any(SyntaxKind.PublicKeyword), out result) ||
                    TryCompare(x, y, p => p.Modifiers.Any(SyntaxKind.InternalKeyword), out result) ||
                    TryCompare(x, y, p => p.Modifiers.Any(SyntaxKind.ProtectedKeyword), out result) ||
                    TryCompare(x, y, p => p.Modifiers.Any(SyntaxKind.PrivateKeyword), out result) ||
                    TryCompare(x, y, p => p.Modifiers.Any(SyntaxKind.StaticKeyword), out result) ||
                    TryCompare(x, y, p => !(p is IndexerDeclarationSyntax), out result) ||
                    TryCompare(x, y, IsGetOnly, out result) ||
                    TryCompare(x, y, IsCalculated, out result) ||
                    TryCompare(xSetter, ySetter, p => p?.Modifiers.Any(SyntaxKind.PrivateKeyword) == true, out result) ||
                    TryCompare(xSetter, ySetter, p => p?.Modifiers.Any(SyntaxKind.ProtectedKeyword) == true, out result) ||
                    TryCompare(xSetter, ySetter, p => p?.Modifiers.Any(SyntaxKind.InternalKeyword) == true, out result))
                {
                    return result;
                }

                return 0;
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

            private static bool IsGetOnly(BasePropertyDeclarationSyntax property)
            {
                AccessorDeclarationSyntax getter;
                if (!property.TryGetGetAccessorDeclaration(out getter) ||
                    getter.Body != null)
                {
                    return false;
                }

                AccessorDeclarationSyntax _;
                return !property.TryGetSetAccessorDeclaration(out _);
            }

            private static bool IsCalculated(BasePropertyDeclarationSyntax property)
            {
                AccessorDeclarationSyntax _;
                if (property.TryGetSetAccessorDeclaration(out _))
                {
                    return false;
                }

                AccessorDeclarationSyntax getter;
                if (property.TryGetGetAccessorDeclaration(out getter) && getter.Body != null)
                {
                    return true;
                }

                return (property as PropertyDeclarationSyntax)?.ExpressionBody != null;
            }
        }
    }
}