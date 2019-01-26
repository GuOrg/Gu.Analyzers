namespace Gu.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ObjectCreationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GU0005ExceptionArgumentsPositions.Descriptor,
            GU0013CheckNameInThrow.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ObjectCreationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                context.SemanticModel.TryGetSymbol(objectCreation, context.CancellationToken, out var ctor))
            {
                if (objectCreation.ArgumentList is ArgumentListSyntax argumentList &&
                    argumentList.Arguments.Count > 0 &&
                    context.ContainingSymbol is IMethodSymbol method &&
                    ctor.ContainingType.IsEither(KnownSymbol.ArgumentException, KnownSymbol.ArgumentNullException, KnownSymbol.ArgumentOutOfRangeException) &&
                    ctor.TryFindParameter("paramName", out var nameParameter))
                {
                    if (objectCreation.TryFindArgument(nameParameter, out var nameArgument) &&
                        objectCreation.Parent is ThrowExpressionSyntax throwExpression &&
                        throwExpression.Parent is BinaryExpressionSyntax binary &&
                        binary.IsKind(SyntaxKind.CoalesceExpression) &&
                        nameArgument.TryGetStringValue(context.SemanticModel, context.CancellationToken, out var name) &&
                        binary.Left is IdentifierNameSyntax identifierName &&
                        identifierName.Identifier.ValueText != name)
                    {
                        var properties = ImmutableDictionary.CreateRange(
                            new[] { new KeyValuePair<string, string>("Name", identifierName.Identifier.ValueText) });
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                GU0013CheckNameInThrow.Descriptor,
                                nameArgument.GetLocation(),
                                properties));
                    }

                    if (TryGetWithParameterName(argumentList, method.Parameters, out var argument) &&
                        argument.NameColon == null &&
                        objectCreation.ArgumentList.Arguments.IndexOf(argument) != ctor.Parameters.IndexOf(nameParameter))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                GU0005ExceptionArgumentsPositions.Descriptor,
                                argument.GetLocation()));
                    }
                }
            }
        }

        private static bool TryGetWithParameterName(ArgumentListSyntax argumentList, ImmutableArray<IParameterSymbol> parameters, out ArgumentSyntax argument)
        {
            argument = null;
            foreach (var arg in argumentList.Arguments)
            {
                if (arg.Expression is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                    parameters.TryFirst(x => x.Name == literal.Token.ValueText, out _))
                {
                    if (argument != null)
                    {
                        return false;
                    }

                    argument = arg;
                }

                if (arg.TryGetNameOf(out var name) &&
                    parameters.TryFirst(x => x.Name == name, out _))
                {
                    if (argument != null)
                    {
                        return false;
                    }

                    argument = arg;
                }
            }

            return argument != null;
        }
    }
}
