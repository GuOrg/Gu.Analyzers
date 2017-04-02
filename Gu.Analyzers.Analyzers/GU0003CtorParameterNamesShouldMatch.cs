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
    internal class GU0003CtorParameterNamesShouldMatch : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0003";
        private const string Title = "Name the parameters to match the members.";
        private const string MessageFormat = "Name the parameters to match the members.";
        private const string Description = "Name the constructor parameters to match the properties or fields.";
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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleConstructor, SyntaxKind.ConstructorDeclaration);
        }

        private static void HandleConstructor(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var constructorDeclarationSyntax = (ConstructorDeclarationSyntax)context.Node;
            if (constructorDeclarationSyntax.ParameterList.Parameters.Count == 0)
            {
                return;
            }

            using (var pooled = ConstructorAssignmentsWalker.Create(constructorDeclarationSyntax, context.SemanticModel, context.CancellationToken))
            {
                foreach (var kvp in pooled.Item.ParameterNameMap)
                {
                    if (kvp.Value != null && !IsMatch(kvp.Key.Identifier, kvp.Value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, kvp.Key.Identifier.GetLocation()));
                    }
                }
            }
        }

        private static bool IsMatch(SyntaxToken identifier, string name)
        {
            if (identifier.ValueText == name)
            {
                return true;
            }

            return false;
        }

        internal sealed class ConstructorAssignmentsWalker : CSharpSyntaxWalker
        {
            internal readonly Dictionary<ParameterSyntax, string> ParameterNameMap = new Dictionary<ParameterSyntax, string>();

            private static readonly Pool<ConstructorAssignmentsWalker> Cache = new Pool<ConstructorAssignmentsWalker>(
                () => new ConstructorAssignmentsWalker(),
                x =>
                {
                    x.ParameterNameMap.Clear();
                    x.constructor = null;
                    x.semanticModel = null;
                    x.cancellationToken = CancellationToken.None;
                });

            private ConstructorDeclarationSyntax constructor;
            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private ConstructorAssignmentsWalker()
            {
            }

            public static Pool<ConstructorAssignmentsWalker>.Pooled Create(
                ConstructorDeclarationSyntax constructor,
                SemanticModel semanticModel,
                CancellationToken cancellationToken)
            {
                var pooled = Cache.GetOrCreate();
                pooled.Item.constructor = constructor;
                pooled.Item.semanticModel = semanticModel;
                pooled.Item.cancellationToken = cancellationToken;
                pooled.Item.Visit(constructor);
                return pooled;
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                var right = node.Right as IdentifierNameSyntax;
                if (right != null)
                {
                    var rightSymbol = this.semanticModel.GetSymbolSafe(right, this.cancellationToken);
                    if (rightSymbol is IParameterSymbol)
                    {
                        var left = node.Left;
                        if (left is IdentifierNameSyntax ||
                            (left as MemberAccessExpressionSyntax)?.Expression is ThisExpressionSyntax ||
                            (left as MemberAccessExpressionSyntax)?.Expression is BaseExpressionSyntax)
                        {
                            if (this.constructor.ParameterList.Parameters.TryGetSingle(x => x.Identifier.ValueText == right.Identifier.ValueText, out ParameterSyntax match))
                            {
                                var symbol = this.semanticModel.GetSymbolSafe(node.Left, this.cancellationToken);
                                if (this.ParameterNameMap.ContainsKey(match))
                                {
                                    this.ParameterNameMap[match] = null;
                                }
                                else
                                {
                                    this.ParameterNameMap.Add(match, ParameterName(symbol));
                                }
                            }
                        }
                    }
                }

                base.VisitAssignmentExpression(node);
            }

            public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
            {
                var ctor = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                if (ctor != null)
                {
                    for (var i = 0; i < node.ArgumentList.Arguments.Count; i++)
                    {
                        var arg = node.ArgumentList.Arguments[i].Expression as IdentifierNameSyntax;
                        if (this.constructor.ParameterList.Parameters.TryGetSingle(x => x.Identifier.ValueText == arg?.Identifier.ValueText, out ParameterSyntax match))
                        {
                            if (this.ParameterNameMap.ContainsKey(match))
                            {
                                this.ParameterNameMap[match] = null;
                            }
                            else
                            {
                                if (ctor.Parameters.Length - 1 <= i &&
                                    ctor.Parameters[ctor.Parameters.Length - 1].IsParams)
                                {
                                    this.ParameterNameMap.Add(match, null);
                                }
                                else
                                {
                                    this.ParameterNameMap.Add(match, ctor.Parameters[i].Name);
                                }
                            }
                        }
                    }
                }

                base.VisitConstructorInitializer(node);
            }

            private static string ParameterName(ISymbol symbol)
            {
                if (symbol is IFieldSymbol field)
                {
                    if (IsAllCaps(field.Name))
                    {
                        return field.Name.ToLowerInvariant();
                    }

                    return FirstCharLowercase(TrimLeadingUnderscore(field.Name));
                }

                if (symbol is IPropertySymbol property)
                {
                    if (IsAllCaps(property.Name))
                    {
                        return property.Name.ToLowerInvariant();
                    }

                    return FirstCharLowercase(property.Name);
                }

                return null;
            }

            private static bool IsAllCaps(string name)
            {
                foreach (var c in name)
                {
                    if (char.IsLetter(c) && char.IsLower(c))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static string TrimLeadingUnderscore(string text)
            {
                if (text[0] != '_' || text.Length == 1)
                {
                    return text;
                }

                return text.Substring(1);
            }

            private static string FirstCharLowercase(string text)
            {
                if (char.IsLower(text[0]))
                {
                    return text;
                }

                var charArray = text.ToCharArray();
                charArray[0] = char.ToLower(charArray[0]);
                return new string(charArray);
            }
        }
    }
}