﻿namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using Gu.Roslyn.AnalyzerExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameConstructorParameterFix))]
[Shared]
internal class RenameConstructorParameterFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Descriptors.GU0003CtorParameterNamesShouldMatch.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                         .ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            if (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) is ParameterSyntax parameterSyntax &&
                semanticModel is { } &&
                semanticModel.TryGetSymbol(parameterSyntax, context.CancellationToken, out var parameter) &&
                diagnostic.Properties.TryGetValue("Name", out var name))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Rename to '{name}'",
                        cancellationToken => Renamer.RenameSymbolAsync(
                            context.Document.Project.Solution,
                            parameter,
                            default,
                            name!,
                            cancellationToken),
                        nameof(NameArgumentsFix)),
                    diagnostic);
            }
        }
    }
}
