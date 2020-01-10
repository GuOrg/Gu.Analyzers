﻿namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableFix))]
    [Shared]
    internal class NullableFix : DocumentEditorCodeFixProvider
    {
        private static readonly UsingDirectiveSyntax UsingSystemDiagnosticsCodeAnalysis = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Diagnostics.CodeAnalysis"));
        private static readonly AttributeListSyntax NotNullWhenTrue = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("NotNullWhen"),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))))));

        private static readonly AttributeListSyntax MaybeNullWhenFalse = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("MaybeNullWhen"),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))))));

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS8600", "CS8601", "CS8618", "CS8625", "CS8653");

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ExpressionSyntax? expression))
                {
                    if (TryFindLocalOrParameter() is { } localOrParameter &&
                        TryFindOutParameter(localOrParameter.Identifier.ValueText, out var outParameter))
                    {
                        if (diagnostic.Id == "CS8625" ||
                            diagnostic.Id == "CS8601")
                        {
                            context.RegisterCodeFix(
                                "[NotNullWhen(true)]",
                                (editor, _) => editor.ReplaceNode(
                                    outParameter!,
                                    x => outParameter!.WithAttributeList(NotNullWhenTrue)
                                                      .WithType(SyntaxFactory.NullableType(outParameter!.Type)))
                                                     .AddUsing(UsingSystemDiagnosticsCodeAnalysis),
                                "[NotNullWhen(true)]",
                                diagnostic);
                        }
                        else if (diagnostic.Id == "CS8653")
                        {
                            context.RegisterCodeFix(
                                "[MaybeNullWhen(false)]",
                                (editor, _) => editor.ReplaceNode(outParameter!, x => outParameter!.WithAttributeList(MaybeNullWhenFalse))
                                                     .ReplaceNode(expression, x => SyntaxFactory.ParseExpression("default!"))
                                                     .AddUsing(UsingSystemDiagnosticsCodeAnalysis),
                                "[MaybeNullWhen(false)]",
                                diagnostic);
                        }
                    }

                    if (expression.Parent is EqualsValueClauseSyntax { Parent: ParameterSyntax optionalParameter })
                    {
                        context.RegisterCodeFix(
                            optionalParameter.Type + "?",
                            (editor, _) => editor.ReplaceNode(
                                optionalParameter.Type,
                                x => SyntaxFactory.NullableType(x)),
                            "?",
                            diagnostic);
                    }

                    if (expression is DeclarationExpressionSyntax { Type: { } type, Parent: ArgumentSyntax _ } &&
                        diagnostic.Id == "CS8600")
                    {
                        context.RegisterCodeFix(
                            type + "?",
                            (editor, _) => editor.ReplaceNode(
                                type,
                                x => SyntaxFactory.NullableType(x)),
                            "out?",
                            diagnostic);
                    }

                    IdentifierNameSyntax? TryFindLocalOrParameter()
                    {
                        return expression switch
                        {
                            { Parent: AssignmentExpressionSyntax { Left: IdentifierNameSyntax local } } => local,
                            { Parent: ArgumentSyntax { Expression: IdentifierNameSyntax arg } } => arg,
                            _ => null!,
                        };
                    }

                    bool TryFindOutParameter(string name, out ParameterSyntax? result)
                    {
                        result = null!;
                        return expression!.FirstAncestor<MethodDeclarationSyntax>() is { } method &&
                               method.ReturnType == KnownSymbol.Boolean &&
                               method.TryFindParameter(name, out result) &&
                               result.Modifiers.Any(SyntaxKind.OutKeyword);
                    }
                }
                else if (diagnostic.Id == "CS8618" &&
                         syntaxRoot.TryFindNodeOrAncestor(diagnostic, out MemberDeclarationSyntax? member))
                {
                    if (FindType(member) is { } type)
                    {
                        context.RegisterCodeFix(
                            "Declare as nullable.",
                            (editor, _) => editor.ReplaceNode(
                                type,
                                x => SyntaxFactory.NullableType(x)),
                            "?",
                            diagnostic);
                    }

                    TypeSyntax? FindType(MemberDeclarationSyntax candidate)
                    {
                        return candidate switch
                        {
                            EventDeclarationSyntax { Type: { } t } => t,
                            EventFieldDeclarationSyntax { Declaration: { Type: { } t } } => t,
                            ConstructorDeclarationSyntax { Parent: TypeDeclarationSyntax typeDeclaration }
                            when Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture), "Non-nullable event '(?<name>[^']+)' is uninitialized") is { Success: true } match &&
                                 typeDeclaration.TryFindEvent(match.Groups["name"].Value, out member)
                            => FindType(member),
                            _ => null,
                        };
                    }
                }
            }
        }
    }
}