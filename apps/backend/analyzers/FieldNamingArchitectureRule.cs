using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

internal sealed class FieldNamingArchitectureRule : IArchitectureRule
{
    public void Register(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IFieldSymbol fieldSymbol)
            return;

        if (fieldSymbol.DeclaredAccessibility is not Accessibility.Private
            || !fieldSymbol.IsReadOnly
            || fieldSymbol.IsStatic
            || fieldSymbol.IsConst)
            return;

        var declaringSyntax = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (declaringSyntax is null)
            return;

        var filePath = ArchitecturePaths.Normalize(declaringSyntax.SyntaxTree.FilePath);
        if (!ArchitecturePaths.IsBackendSourceFile(filePath))
            return;

        var expectedName = NamingConvention.GetExpectedDependencyFieldName(fieldSymbol.Type);
        if (expectedName is null || fieldSymbol.Name == expectedName)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ArchitectureDiagnostics.DependencyFieldNamingRule,
            declaringSyntax.GetSyntax(context.CancellationToken).GetLocation(),
            fieldSymbol.Name,
            fieldSymbol.Type.Name,
            expectedName));
    }
}
