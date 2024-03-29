﻿namespace Gu.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.CodeFixExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocsFix))]
[Shared]
internal class DocsFix : DocumentEditorCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS1591", "SA1611", "SA1614", "SA1618", Descriptors.GU0100WrongCrefType.Id);

    protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                         .ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            if (syntaxRoot is { } &&
                semanticModel is { } &&
                syntaxRoot.TryFindNodeOrAncestor(diagnostic, out BaseMethodDeclarationSyntax? methodDeclaration))
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out ParameterSyntax? parameter) &&
                    methodDeclaration.TryGetDocumentationComment(out var docs))
                {
                    if (StandardDocs.TryGet(parameter, out var text))
                    {
                        context.RegisterCodeFix(
                            "Generate standard xml documentation for parameter.",
                            (editor, _) => editor.ReplaceNode(
                                docs,
                                x => x.WithParamText(parameter.Identifier.ValueText, text)),
                            text,
                            diagnostic);
                    }
                    else if (parameter.Type is PredefinedTypeSyntax)
                    {
                        context.RegisterCodeFix(
                            "Generate empty xml documentation for parameter.",
                            (editor, _) => editor.ReplaceNode(
                                docs,
                                x => x.WithParamText(parameter.Identifier.ValueText, string.Empty)),
                            nameof(DocsFix),
                            diagnostic);
                    }
                    else if (parameter.Type is { })
                    {
                        context.RegisterCodeFix(
                            "Generate useless xml documentation for parameter.",
                            (editor, _) => editor.ReplaceNode(
                                docs,
                                x => x.WithParamText(parameter.Identifier.ValueText, $"The <see cref=\"{CrefType(parameter.Type)}\"/>.")),
                            nameof(DocsFix),
                            diagnostic);
                    }
                }
                else if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out TypeParameterSyntax? typeParameter) &&
                         methodDeclaration.TryGetDocumentationComment(out docs))
                {
                    if (TryGetTypeParameterText(methodDeclaration, typeParameter, semanticModel, context.CancellationToken, out var text))
                    {
                        context.RegisterCodeFix(
                            "Generate useless xml documentation for type parameter.",
                            (editor, _) => editor.ReplaceNode(
                                docs,
                                x => x.WithTypeParamText(typeParameter.Identifier.ValueText, text)),
                            nameof(DocsFix),
                            diagnostic);
                    }
                    else
                    {
                        context.RegisterCodeFix(
                            "Generate empty xml documentation for type parameter.",
                            (editor, _) => editor.ReplaceNode(
                                docs,
                                x => x.WithTypeParamText(typeParameter.Identifier.ValueText, string.Empty)),
                            nameof(DocsFix),
                            diagnostic);
                    }
                }
                else if (TryFindParamDoc(out var element) &&
                         element.TryGetNameAttribute(out var name) &&
                         methodDeclaration.TryFindParameter(name.Identifier.Identifier.ValueText, out parameter))
                {
                    if (StandardDocs.TryGet(parameter, out var text))
                    {
                        context.RegisterCodeFix(
                            "Generate standard xml documentation for parameter.",
                            (editor, _) => editor.ReplaceNode(
                                element,
                                x => WithText(x, text)),
                            text,
                            diagnostic);
                    }
                    else if (parameter.Type is { })
                    {
                        context.RegisterCodeFix(
                            "Generate useless xml documentation for parameter.",
                            (editor, _) => editor.ReplaceNode(
                                element,
                                x => WithText(x, $"The <see cref=\"{CrefType(parameter.Type)}\"/>.")),
                            nameof(DocsFix),
                            diagnostic);
                    }
                }
                else if (diagnostic.Id == "CS1591" &&
                         syntaxRoot.TryFindNodeOrAncestor(diagnostic, out OperatorDeclarationSyntax? operatorDeclaration) &&
                         operatorDeclaration.ParameterList is { Parameters.Count: 2 } parameterList &&
                         parameterList.Parameters.TryElementAt(0, out var left) &&
                         left is { Type: { } leftType } &&
                         parameterList.Parameters.TryElementAt(1, out var right) &&
                         right is { Type: { } rightType })
                {
                    if (operatorDeclaration.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken))
                    {
                        context.RegisterCodeFix(
                            "Generate standard xml documentation for operator.",
                            (editor, _) => editor.ReplaceNode(
                                operatorDeclaration,
                                x => x.WithDocumentationText(
                                    StringBuilderPool.Borrow()
                                                     .AppendLine("/// <summary>Check if <paramref name=\"left\"/> is equal to <paramref name=\"right\"/>.</summary>")
                                                     .AppendLine($"/// <param name=\"left\">The left <see cref=\"{CrefType(leftType)}\"/>.</param>")
                                                     .AppendLine($"/// <param name=\"right\">The right <see cref=\"{CrefType(rightType)}\"/>.</param>")
                                                     .AppendLine("/// <returns>True if <paramref name=\"left\"/> is equal to <paramref name=\"right\"/>.</returns>")
                                                     .Return())),
                            nameof(DocsFix),
                            diagnostic);
                    }
                    else if (operatorDeclaration.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken))
                    {
                        context.RegisterCodeFix(
                            "Generate standard xml documentation for operator.",
                            (editor, _) => editor.ReplaceNode(
                                operatorDeclaration,
                                x => x.WithDocumentationText(
                                    StringBuilderPool.Borrow()
                                                     .AppendLine("/// <summary>Check if <paramref name=\"left\"/> is not equal to <paramref name=\"right\"/>.</summary>")
                                                     .AppendLine($"/// <param name=\"left\">The left <see cref=\"{CrefType(leftType)}\"/>.</param>")
                                                     .AppendLine($"/// <param name=\"right\">The right <see cref=\"{CrefType(rightType)}\"/>.</param>")
                                                     .AppendLine("/// <returns>True if <paramref name=\"left\"/> is not equal to <paramref name=\"right\"/>.</returns>")
                                                     .Return())),
                            nameof(DocsFix),
                            diagnostic);
                    }
                }
            }

            bool TryFindParamDoc(out XmlElementSyntax result)
            {
                switch (syntaxRoot.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true))
                {
                    case XmlElementSyntax element:
                        result = element;
                        return true;
                    case { } node when node.TryFirstAncestor(out XmlElementSyntax? element):
                        result = element;
                        return true;
                    default:
                        result = null!;
                        return false;
                }
            }

            static string CrefType(TypeSyntax type)
            {
                return SyntaxFactory.TypeCref(type)
                                    .ToString()
                                    .Replace('<', '{')
                                    .Replace('>', '}');
            }
        }
    }

    private static bool TryGetTypeParameterText(BaseMethodDeclarationSyntax methodDeclaration, TypeParameterSyntax typeParameter, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out string? text)
    {
        text = null;
        if (methodDeclaration.ParameterList is { Parameters: { } parameters })
        {
            foreach (var parameter in parameters)
            {
                if (IsParameterType(parameter.Type))
                {
                    if (text != null)
                    {
                        text = null;
                        return false;
                    }

                    text = $"The type of <paramref name=\"{parameter.Identifier.Text}\"/>.";
                }
                else if (parameter.Type is ArrayTypeSyntax arrayType &&
                         IsParameterType(arrayType.ElementType))
                {
                    if (text != null)
                    {
                        text = null;
                        return false;
                    }

                    text = $"The type of the elements in <paramref name=\"{parameter.Identifier.Text}\"/>.";
                }
                else if (parameter.Type is { } parameterType &&
                         semanticModel.GetType(parameterType, cancellationToken) is INamedTypeSymbol { IsGenericType: true } type &&
                         type.Interfaces.TrySingle(x => x.MetadataName == "IEnumerable`1", out _))
                {
                    if (text != null)
                    {
                        text = null;
                        return false;
                    }

                    text = $"The type of the elements in <paramref name=\"{parameter.Identifier.Text}\"/>.";
                }
            }
        }

        return text != null;

        bool IsParameterType(TypeSyntax? typeSyntax)
        {
            return typeSyntax is SimpleNameSyntax simple &&
                   simple.Identifier.Text == typeParameter.Identifier.Text;
        }
    }

    private static XmlElementSyntax WithText(XmlElementSyntax element, string text)
    {
        return element.WithContent(Parse.XmlElementSyntax($"<param>{text}</param>").Content);
    }
}
