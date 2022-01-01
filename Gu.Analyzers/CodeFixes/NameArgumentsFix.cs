namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.CodeFixExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NameArgumentsFix))]
[Shared]
internal class NameArgumentsFix : DocumentEditorCodeFixProvider
{
    private static readonly SyntaxTriviaList SpaceTrivia = SyntaxTriviaList.Empty.Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "));

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Descriptors.GU0001NameArguments.Id,
        Descriptors.GU0009UseNamedParametersForBooleans.Id);

    protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                         .ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id == Descriptors.GU0001NameArguments.Id &&
                syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) is ArgumentListSyntax { Parent: { } parent } arguments &&
                !HasAnyNamedArgument(arguments) &&
                semanticModel is { } &&
                semanticModel.TryGetSymbol(parent, context.CancellationToken, out IMethodSymbol? method))
            {
                context.RegisterCodeFix(
                    "Name arguments",
                    (editor, _) => ApplyFix(editor, method, arguments, 0),
                    this.GetType(),
                    diagnostic);
            }
            else if (diagnostic.Id == Descriptors.GU0009UseNamedParametersForBooleans.Id &&
                     syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: { } } argumentList } boolArgument &&
                     !HasAnyNamedArgument(argumentList) &&
                     semanticModel is { } &&
                     argumentList.Parent is { } &&
                     semanticModel.TryGetSymbol(argumentList.Parent, context.CancellationToken, out method))
            {
                context.RegisterCodeFix(
                    "Name arguments",
                    (editor, _) => ApplyFix(editor, method, argumentList, argumentList.Arguments.IndexOf(boolArgument)),
                    this.GetType(),
                    diagnostic);
            }
        }
    }

    private static void ApplyFix(DocumentEditor editor, IMethodSymbol method, ArgumentListSyntax arguments, int startIndex)
    {
        for (var i = startIndex; i < arguments.Arguments.Count; i++)
        {
            var argument = arguments.Arguments[i];

            editor.ReplaceNode(
                argument,
                argument.WithLeadingTrivia(SpaceTrivia)
                        .WithNameColon(
                            SyntaxFactory.NameColon(method.Parameters[i].Name)
                                         .WithLeadingTriviaFrom(argument))
                        .WithTrailingTriviaFrom(argument));
        }
    }

    private static bool HasAnyNamedArgument(ArgumentListSyntax argumentList)
    {
        foreach (var argument in argumentList.Arguments)
        {
            if (argument.NameColon != null)
            {
                return true;
            }
        }

        return false;
    }
}