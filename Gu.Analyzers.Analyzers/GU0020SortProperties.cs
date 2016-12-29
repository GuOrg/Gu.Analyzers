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

            var property = (IPropertySymbol)context.ContainingSymbol;
            var neighbors = GetNeighbors((BasePropertyDeclarationSyntax)context.Node);
            if (neighbors.Before != null)
            {
                var other = context.SemanticModel.GetDeclaredSymbolSafe(neighbors.Before, context.CancellationToken);
                if (PropertyPositionComparer.Default.Compare(other, property) > 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }

            if (neighbors.After != null)
            {
                var other = context.SemanticModel.GetDeclaredSymbolSafe(neighbors.After, context.CancellationToken);
                if (PropertyPositionComparer.Default.Compare(other, property) < 0)
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

        internal struct Neighbors
        {
            internal readonly BasePropertyDeclarationSyntax Before;
            internal readonly BasePropertyDeclarationSyntax After;

            public Neighbors(BasePropertyDeclarationSyntax before, BasePropertyDeclarationSyntax after)
            {
                this.Before = before;
                this.After = after;
            }
        }

        internal sealed class SortedProperties
        {
            private static readonly Pool<SortedProperties> Cache = new Pool<SortedProperties>(
                () => new SortedProperties(),
                x =>
                {
                    x.documentOrder.Clear();

                    x.sorted.Clear();
                });

            private readonly List<IPropertySymbol> documentOrder = new List<IPropertySymbol>();
            private readonly List<IPropertySymbol> sorted = new List<IPropertySymbol>();

            private SortedProperties()
            {
            }

            public IReadOnlyList<IPropertySymbol> Sorted => this.sorted;

            public bool IsSorted
            {
                get
                {
                    for (var i = 0; i < this.documentOrder.Count; i++)
                    {
                        if (!ReferenceEquals(this.documentOrder[i], this.sorted[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            public static Pool<SortedProperties>.Pooled Create(ITypeSymbol type)
            {
                var pooled = Cache.GetOrCreate();
                pooled.Item.documentOrder.AddRange(type.GetMembers().OfType<IPropertySymbol>());
                pooled.Item.sorted.AddRange(pooled.Item.documentOrder);
                pooled.Item.sorted.Sort(PropertyPositionComparer.Default);
                return pooled;
            }

            public bool IsAtCorrectPosition(IPropertySymbol property)
            {
                return this.documentOrder.IndexOf(property) == this.sorted.IndexOf(property);
            }

            public int IndexOfSorted(IPropertySymbol property)
            {
                return this.sorted.IndexOf(property);
            }
        }

        internal class PropertyPositionComparer : IComparer<IPropertySymbol>
        {
            public static readonly PropertyPositionComparer Default = new PropertyPositionComparer();

            public int Compare(IPropertySymbol x, IPropertySymbol y)
            {
                int result;
                if (TryCompare(x, y, p => p.DeclaredAccessibility == Accessibility.Public, out result) ||
                    TryCompare(x, y, p => p.DeclaredAccessibility == Accessibility.Internal, out result) ||
                    TryCompare(x, y, p => p.DeclaredAccessibility == Accessibility.Protected, out result) ||
                    TryCompare(x, y, p => p.DeclaredAccessibility == Accessibility.Private, out result) ||
                    TryCompare(x, y, p => p.IsStatic, out result) ||
                    TryCompare(x, y, p => !p.IsIndexer, out result))
                {
                    return result;
                }

                var xDeclaration = (BasePropertyDeclarationSyntax)x.DeclaringSyntaxReferences[0].GetSyntax();
                var yDeclaration = (BasePropertyDeclarationSyntax)y.DeclaringSyntaxReferences[0].GetSyntax();
                if (TryCompare(xDeclaration, yDeclaration, IsGetOnly, out result) ||
                    TryCompare(xDeclaration, yDeclaration, IsCalculated, out result) ||
                    TryCompare(x, y, p => p.SetMethod?.DeclaredAccessibility == Accessibility.Private, out result) ||
                    TryCompare(x, y, p => p.SetMethod?.DeclaredAccessibility == Accessibility.Protected, out result) ||
                    TryCompare(x, y, p => p.SetMethod?.DeclaredAccessibility == Accessibility.Internal, out result) ||
                    TryCompare(x, y, p => p.SetMethod?.DeclaredAccessibility == Accessibility.Public, out result))
                {
                    return result;
                }

                return 0;
            }

            private static bool TryCompare(BasePropertyDeclarationSyntax x, BasePropertyDeclarationSyntax y, Func<BasePropertyDeclarationSyntax, bool> criteria, out int result)
            {
                return TryCompare(criteria(x), criteria(y), out result);
            }

            private static bool TryCompare(IPropertySymbol x, IPropertySymbol y, Func<IPropertySymbol, bool> criteria, out int result)
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
                if (property.TryGetGetAccessorDeclaration(out getter) && getter.Body == null)
                {
                    AccessorDeclarationSyntax _;
                    if (property.TryGetSetAccessorDeclaration(out _))
                    {
                        return false;
                    }

                    return true;
                }

                return false;
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