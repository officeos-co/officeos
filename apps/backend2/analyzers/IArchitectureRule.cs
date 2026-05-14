using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

internal interface IArchitectureRule
{
    void Register(AnalysisContext context);
}
