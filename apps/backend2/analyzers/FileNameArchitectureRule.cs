using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

internal sealed class FileNameArchitectureRule : IArchitectureRule
{
    public void Register(AnalysisContext context)
    {
        context.RegisterSyntaxTreeAction(AnalyzeFileName);
    }

    private static void AnalyzeFileName(SyntaxTreeAnalysisContext context)
    {
        var filePath = ArchitecturePaths.Normalize(context.Tree.FilePath);
        if (!ArchitecturePaths.IsFeatureFile(filePath))
            return;

        var fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith("Types.cs", StringComparison.Ordinal))
            ReportFileDiagnostic(context, ArchitectureDiagnostics.BroadTypesFileRule, fileName);

        if (ArchitecturePaths.IsDomainFile(filePath) && fileName.EndsWith("Dto.cs", StringComparison.Ordinal))
            ReportFileDiagnostic(context, ArchitectureDiagnostics.DomainDtoTypeRule, Path.GetFileNameWithoutExtension(fileName));
    }

    private static void ReportFileDiagnostic(SyntaxTreeAnalysisContext context, DiagnosticDescriptor descriptor, string argument)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var location = root.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, argument));
    }
}
