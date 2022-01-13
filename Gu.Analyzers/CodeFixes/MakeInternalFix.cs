namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeInternalFix))]
[Shared]
internal class MakeInternalFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Descriptors.GU0072AllTypesShouldBeInternal.Id,
        Descriptors.GU0073MemberShouldBeInternal.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (syntaxRoot?.FindToken(diagnostic.Location.SourceSpan.Start) is { } token &&
                token.IsKind(SyntaxKind.PublicKeyword))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Make internal.",
                        _ => Task.FromResult(
                            context.Document.WithSyntaxRoot(
                                syntaxRoot.ReplaceToken(
                                    token,
                                    SyntaxFactory.Token(SyntaxKind.InternalKeyword).WithTriviaFrom(token)))),
                        nameof(MakeInternalFix)),
                    diagnostic);
            }
        }
    }
}
