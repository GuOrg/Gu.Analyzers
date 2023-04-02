namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class ObjectCreationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.GU0005ExceptionArgumentsPositions,
        Descriptors.GU0013TrowForCorrectParameter);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ObjectCreationExpression);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.Node is ObjectCreationExpressionSyntax { ArgumentList.Arguments: { } arguments } objectCreation &&
            arguments.Count > 0 &&
            context.SemanticModel.TryGetSymbol(objectCreation, context.CancellationToken, out var ctor) &&
            context.ContainingSymbol is IMethodSymbol method &&
            ctor.ContainingType.IsEither(KnownSymbols.ArgumentException, KnownSymbols.ArgumentNullException, KnownSymbols.ArgumentOutOfRangeException) &&
            ctor.TryFindParameter("paramName", out var nameParameter))
        {
            if (objectCreation.FindArgument(nameParameter) is { } nameArgument &&
                objectCreation.Parent is ThrowExpressionSyntax { Parent: BinaryExpressionSyntax { Left: IdentifierNameSyntax left, OperatorToken.ValueText: "??" } } &&
                nameArgument.TryGetStringValue(context.SemanticModel, context.CancellationToken, out var name) &&
                left.Identifier.ValueText != name)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.GU0013TrowForCorrectParameter,
                        GetLocation(),
                        ImmutableDictionary<string, string?>.Empty.Add(nameof(IdentifierNameSyntax), left.Identifier.ValueText)));

                Location GetLocation()
                {
                    return nameArgument is { Expression: InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } invocation } &&
                           invocation.IsNameOf() &&
                           invocation.ArgumentList.Arguments.TrySingle(out var nameofArg)
                        ? nameofArg.GetLocation()
                        : nameArgument.GetLocation();
                }
            }

            if (TryGetWithParameterName(arguments, method.Parameters, out var argument) &&
                argument.NameColon is null &&
                objectCreation.ArgumentList is { } argumentList &&
                argumentList.Arguments.IndexOf(argument) != ctor.Parameters.IndexOf(nameParameter))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.GU0005ExceptionArgumentsPositions,
                        argument.GetLocation()));
            }
        }
    }

    private static bool TryGetWithParameterName(SeparatedSyntaxList<ArgumentSyntax> arguments, ImmutableArray<IParameterSymbol> parameters, [NotNullWhen(true)] out ArgumentSyntax? argument)
    {
        argument = null;
        foreach (var arg in arguments)
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
