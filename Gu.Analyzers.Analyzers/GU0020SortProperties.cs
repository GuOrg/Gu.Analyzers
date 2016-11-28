namespace Gu.Analyzers
{
    using System;
    using System.Collections.Concurrent;
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
        private const string MessageFormat = "Sort properties.";
        private const string Description = "Sort properties by StyleCop rules then by mutability.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.Correctness,
            DiagnosticSeverity.Hidden,
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
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.PropertyDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            var property = (IPropertySymbol)context.ContainingSymbol;
            var type = (ITypeSymbol)property.ContainingType;
            using (var sorted = SortedProperties.Create(type))
            {
                if (!sorted.IsAtCorrectPosition(property))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
                }
            }
        }

        internal sealed class SortedProperties : IDisposable
        {
            private static readonly ConcurrentQueue<SortedProperties> Cache = new ConcurrentQueue<SortedProperties>();
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

            public static SortedProperties Create(ITypeSymbol type)
            {
                SortedProperties sorted;
                if (!Cache.TryDequeue(out sorted))
                {
                    sorted = new SortedProperties();
                }

                sorted.documentOrder.AddRange(type.GetMembers().OfType<IPropertySymbol>());
                sorted.sorted.AddRange(sorted.documentOrder);
                sorted.sorted.Sort(PropertyComparer.Default);
                return sorted;
            }

            public void Dispose()
            {
                this.documentOrder.Clear();
                this.sorted.Clear();
                Cache.Enqueue(this);
            }

            public bool IsAtCorrectPosition(IPropertySymbol property)
            {
                return this.documentOrder.IndexOf(property) == this.sorted.IndexOf(property);
            }

            public int IndexOfSorted(IPropertySymbol property)
            {
                return this.sorted.IndexOf(property);
            }

            private class PropertyComparer : IComparer<IPropertySymbol>
            {
                public static readonly PropertyComparer Default = new PropertyComparer();

                public int Compare(IPropertySymbol x, IPropertySymbol y)
                {
                    int result;
                    if (TryCompare(x.IsIndexer, y.IsIndexer, out result) ||
                        TryCompare(x.IsStatic, y.IsStatic, out result) ||
                        TryCompare(x.DeclaredAccessibility == Accessibility.Public, y.DeclaredAccessibility == Accessibility.Public, out result) ||
                        TryCompare(x.DeclaredAccessibility == Accessibility.Internal, y.DeclaredAccessibility == Accessibility.Internal, out result) ||
                        TryCompare(x.DeclaredAccessibility == Accessibility.Protected, y.DeclaredAccessibility == Accessibility.Protected, out result) ||
                        TryCompare(x.DeclaredAccessibility == Accessibility.Private, y.DeclaredAccessibility == Accessibility.Private, out result) ||
                        TryCompare(x.SetMethod == null, y.SetMethod == null, out result) ||
                        TryCompare(!IsExpressionBody(x), !IsExpressionBody(y), out result) ||
                        TryCompare(x.SetMethod?.DeclaredAccessibility == Accessibility.Private, y.SetMethod?.DeclaredAccessibility == Accessibility.Private, out result) ||
                        TryCompare(x.SetMethod?.DeclaredAccessibility == Accessibility.Protected, y.SetMethod?.DeclaredAccessibility == Accessibility.Protected, out result) ||
                        TryCompare(x.SetMethod?.DeclaredAccessibility == Accessibility.Internal, y.SetMethod?.DeclaredAccessibility == Accessibility.Internal, out result) ||
                        TryCompare(x.SetMethod?.DeclaredAccessibility == Accessibility.Public, y.SetMethod?.DeclaredAccessibility == Accessibility.Public, out result))
                    {
                        return result;
                    }

                    return 0;
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

                private static bool IsExpressionBody(IPropertySymbol property)
                {
                    var declaration = (PropertyDeclarationSyntax)property.DeclaringSyntaxReferences[0].GetSyntax();
                    return declaration.ExpressionBody != null;
                }
            }
        }
    }
}