namespace Gu.Analyzers.Test;

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Microsoft.CodeAnalysis.CSharp.Workspaces 4.5.0 skips default disabled analyzers even if passing SpecificDiagnosticOptions setting severity to warning
/// This is not ideal but the only workaround I could find for the regression in Microsoft.CodeAnalysis.CSharp.Workspaces
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class DefaultEnabledAnalyzer : DiagnosticAnalyzer
{
    private readonly DiagnosticAnalyzer inner;

    internal DefaultEnabledAnalyzer(DiagnosticAnalyzer inner)
    {
        this.inner = inner;
        this.SupportedDiagnostics = EnabledDiagnostics(inner.SupportedDiagnostics);

        static ImmutableArray<DiagnosticDescriptor> EnabledDiagnostics(ImmutableArray<DiagnosticDescriptor> source)
        {
            var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>(source.Length);
            foreach (var diagnostic in source)
            {
                builder.Add(
                    new DiagnosticDescriptor(
                        diagnostic.Id,
                        diagnostic.Title,
                        diagnostic.MessageFormat,
                        diagnostic.Category,
                        diagnostic.DefaultSeverity,
                        isEnabledByDefault: true,
                        diagnostic.Description,
                        diagnostic.HelpLinkUri,
                        diagnostic.CustomTags?.ToArray() ?? Array.Empty<string>()));
            }

            return builder.MoveToImmutable();
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

#pragma warning disable RS1025, RS1026
    public override void Initialize(AnalysisContext context) => this.inner.Initialize(context);
#pragma warning restore RS1025, RS1026
}
