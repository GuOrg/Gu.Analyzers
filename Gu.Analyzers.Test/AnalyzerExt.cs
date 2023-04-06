namespace Gu.Analyzers.Test;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public static class AnalyzerExt
{
    public static T DefaultEnabled<T>(this T analyzer)
        where T : DiagnosticAnalyzer
    {
        var field = analyzer.GetType()
                            .GetField("<SupportedDiagnostics>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic) ??
                    throw new InvalidOperationException("did not find field");
        field.SetValue(analyzer, EnabledDiagnostics(analyzer.SupportedDiagnostics));
        return analyzer;

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
}
