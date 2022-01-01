namespace Gu.Analyzers;

using System.Collections.Immutable;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class GU0050IgnoreEventsWhenSerializing : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.GU0050IgnoreEventsWhenSerializing);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => HandleEventField(c), SyntaxKind.EventFieldDeclaration);
        context.RegisterSyntaxNodeAction(c => HandleField(c), SyntaxKind.FieldDeclaration);
    }

    private static void HandleEventField(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.Node is EventFieldDeclarationSyntax eventFieldDeclaration &&
            context.ContainingSymbol is IEventSymbol eventSymbol &&
            HasSerializableAttribute(eventSymbol.ContainingType) &&
            !Attribute.TryFind(eventFieldDeclaration.AttributeLists, KnownSymbols.NonSerializedAttribute, context.SemanticModel, context.CancellationToken, out _))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0050IgnoreEventsWhenSerializing, eventFieldDeclaration.GetLocation()));
        }
    }

    private static void HandleField(SyntaxNodeAnalysisContext context)
    {
        if (context.IsExcludedFromAnalysis())
        {
            return;
        }

        if (context.Node is FieldDeclarationSyntax fieldDeclaration &&
            context.ContainingSymbol is IFieldSymbol field &&
            field.Type.IsAssignableTo(KnownSymbols.EventHandler, context.Compilation) &&
            HasSerializableAttribute(field.ContainingType) &&
            !Attribute.TryFind(fieldDeclaration.AttributeLists, KnownSymbols.NonSerializedAttribute, context.SemanticModel, context.CancellationToken, out _))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.GU0050IgnoreEventsWhenSerializing, fieldDeclaration.GetLocation()));
        }
    }

    private static bool HasSerializableAttribute(INamedTypeSymbol type)
    {
        return type.GetAttributes()
                   .TryFirst(x => x.AttributeClass == KnownSymbols.SerializableAttribute, out _);
    }
}