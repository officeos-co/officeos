using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BackendArchitectureAnalyzer : DiagnosticAnalyzer
{
    private static readonly IArchitectureRule[] Rules =
    [
        new FileNameArchitectureRule(),
        new UsingDirectiveArchitectureRule(),
        new NamedTypeArchitectureRule(),
        new FieldNamingArchitectureRule(),
        new ParameterArchitectureRule(),
        new UnusedUsingDirectiveArchitectureRule(),
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ArchitectureDiagnostics.All;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        foreach (var rule in Rules)
            rule.Register(context);
    }
}
