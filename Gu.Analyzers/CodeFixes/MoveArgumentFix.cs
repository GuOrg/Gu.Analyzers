namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Gu.Roslyn.CodeFixExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveArgumentFix))]
[Shared]
internal class MoveArgumentFix : DocumentEditorCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Descriptors.GU0002NamedArgumentPositionMatches.Id,
        Descriptors.GU0005ExceptionArgumentsPositions.Id);

    protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            if (syntaxRoot is { } &&
                syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ArgumentListSyntax? argumentList))
            {
                if (diagnostic.Id == Descriptors.GU0002NamedArgumentPositionMatches.Id &&
                    HasWhitespaceTriviaOnly(argumentList))
                {
                    context.RegisterCodeFix(
                        "Move arguments to match parameter positions.",
                        (editor, _) => editor.ReplaceNode(
                            argumentList,
                            x => Replacement(x, editor)),
                        "sort",
                        diagnostic);

                    static ArgumentListSyntax Replacement(ArgumentListSyntax old, DocumentEditor editor)
                    {
                        if (old.Parent is { } &&
                            editor.SemanticModel.GetSpeculativeSymbolInfo(old.SpanStart, old.Parent, SpeculativeBindingOption.BindAsExpression).Symbol is IMethodSymbol method)
                        {
                            return old.WithArguments(
                                SyntaxFactory.SeparatedList(
                                    old.Arguments.OrderBy(x => Index(x)),
                                    old.Arguments.GetSeparators()));

                            int Index(ArgumentSyntax argument)
                            {
                                return argument switch
                                {
                                    { NameColon: { Name: { } } } => ParameterIndex(method, argument.NameColon.Name.Identifier.ValueText),
                                    _ => old.Arguments.IndexOf(argument),
                                };
                            }
                        }

                        return old;
                    }
                }

                if (diagnostic.Id == Descriptors.GU0005ExceptionArgumentsPositions.Id)
                {
                    context.RegisterCodeFix(
                        "Move named argument to match parameter positions.",
                        (editor, _) => editor.ReplaceNode(
                            argumentList,
                            x => Replacement(x, editor)),
                        "move",
                        diagnostic);

                    static ArgumentListSyntax Replacement(ArgumentListSyntax old, DocumentEditor editor)
                    {
                        if (old.Parent is { } &&
                            editor.SemanticModel.GetSpeculativeSymbolInfo(old.SpanStart, old.Parent, SpeculativeBindingOption.BindAsExpression).Symbol is IMethodSymbol method)
                        {
                            var arguments = new ArgumentSyntax[old.Arguments.Count];
                            var messageIndex = ParameterIndex(method, "message");
                            var nameIndex = ParameterIndex(method, "paramName");
                            for (var i = 0; i < old.Arguments.Count; i++)
                            {
                                if (i == messageIndex)
                                {
                                    arguments[nameIndex] = old.Arguments[i];
                                    continue;
                                }

                                if (i == nameIndex)
                                {
                                    arguments[messageIndex] = old.Arguments[i];
                                    continue;
                                }

                                arguments[i] = old.Arguments[i];
                            }

                            return old.WithArguments(
                                SyntaxFactory.SeparatedList(
                                    arguments,
                                    old.Arguments.GetSeparators()));
                        }

                        return old;
                    }
                }
            }
        }
    }

    private static int ParameterIndex(IMethodSymbol method, string name)
    {
        for (var i = 0; i < method.Parameters.Length; i++)
        {
            if (method.Parameters[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool HasWhitespaceTriviaOnly(ArgumentListSyntax arguments)
    {
        foreach (var token in arguments.Arguments.GetWithSeparators())
        {
            if (!IsWhiteSpace(token.GetLeadingTrivia()) ||
                !IsWhiteSpace(token.GetTrailingTrivia()))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWhiteSpace(SyntaxTriviaList trivia)
    {
        foreach (var syntaxTrivia in trivia)
        {
            if (!(syntaxTrivia.IsKind(SyntaxKind.WhitespaceTrivia) ||
                  syntaxTrivia.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                return false;
            }
        }

        return true;
    }
}