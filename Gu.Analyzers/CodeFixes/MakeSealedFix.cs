﻿namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using Gu.Roslyn.CodeFixExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedFix))]
[Shared]
internal class MakeSealedFix : DocumentEditorCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Descriptors.GU0024SealTypeWithDefaultMember.Id,
        Descriptors.GU0025SealTypeWithOverridenEquality.Id);

    protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) is ClassDeclarationSyntax classDeclaration)
            {
                context.RegisterCodeFix(
                    "Make sealed.",
                    (editor, _) => editor.Seal(classDeclaration),
                    "Make sealed.",
                    diagnostic);
            }
        }
    }
}
