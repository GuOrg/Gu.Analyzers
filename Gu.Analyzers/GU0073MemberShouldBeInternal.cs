﻿namespace Gu.Analyzers;

using System.Collections.Immutable;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class GU0073MemberShouldBeInternal : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.GU0073MemberShouldBeInternal);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c => Handle(c),
            SyntaxKind.FieldDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.EventDeclaration,
            SyntaxKind.EventFieldDeclaration,
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.ClassDeclaration);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.ContainingSymbol is { IsOverride: false, DeclaredAccessibility: Accessibility.Public, ContainingType: { } containingType } memberSymbol &&
            containingType.DeclaredAccessibility != Accessibility.Public &&
            TryFindPublicKeyword(out var keyword) &&
            !IsStructCtor() &&
            !ImplementsInterface())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.GU0073MemberShouldBeInternal,
                    keyword.GetLocation(),
                    memberSymbol.Name,
                    memberSymbol.ContainingType.ToMinimalDisplayString(context.SemanticModel, context.Node.SpanStart)));
        }

        bool ImplementsInterface()
        {
            if (memberSymbol.IsStatic)
            {
                return false;
            }

            switch (context.Node.Kind())
            {
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.ClassDeclaration:
                    return false;
            }

            foreach (var @interface in memberSymbol.ContainingType.AllInterfaces)
            {
                foreach (var interfaceMember in @interface.GetMembers(memberSymbol.Name))
                {
                    if (memberSymbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember) != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool IsStructCtor() => containingType.TypeKind == TypeKind.Struct &&
                               context.Node.IsKind(SyntaxKind.ConstructorDeclaration);

        bool TryFindPublicKeyword(out SyntaxToken result)
        {
            switch (context.Node)
            {
                case BaseFieldDeclarationSyntax declaration:
                    return declaration.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.PublicKeyword), out result);
                case BaseMethodDeclarationSyntax declaration:
                    return declaration.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.PublicKeyword), out result);
                case BasePropertyDeclarationSyntax declaration:
                    return declaration.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.PublicKeyword), out result);
                case BaseTypeDeclarationSyntax declaration:
                    return declaration.Modifiers.TrySingle(x => x.IsKind(SyntaxKind.PublicKeyword), out result);
                default:
                    result = default;
                    return false;
            }
        }
    }
}
