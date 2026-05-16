using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

internal sealed class NamespacePathArchitectureRule : IArchitectureRule
{
    public void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeFileScopedNamespace, SyntaxKind.FileScopedNamespaceDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeBlockNamespace, SyntaxKind.NamespaceDeclaration);
    }

    private static void AnalyzeFileScopedNamespace(SyntaxNodeAnalysisContext context)
    {
        var namespaceDeclaration = (FileScopedNamespaceDeclarationSyntax)context.Node;
        AnalyzeNamespace(context, namespaceDeclaration.Name);
    }

    private static void AnalyzeBlockNamespace(SyntaxNodeAnalysisContext context)
    {
        var namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
        AnalyzeNamespace(context, namespaceDeclaration.Name);
    }

    private static void AnalyzeNamespace(SyntaxNodeAnalysisContext context, NameSyntax namespaceNameSyntax)
    {
        var filePath = context.Node.SyntaxTree.FilePath;
        var expectedNamespace = GetExpectedNamespace(filePath);
        if (expectedNamespace is null)
            return;

        var actualNamespace = namespaceNameSyntax.ToString();
        if (actualNamespace.Equals(expectedNamespace, StringComparison.Ordinal))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ArchitectureDiagnostics.NamespacePathRule,
            namespaceNameSyntax.GetLocation(),
            actualNamespace,
            expectedNamespace));
    }

    private static string? GetExpectedNamespace(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var srcIndex = normalizedPath.IndexOf("/src/", StringComparison.OrdinalIgnoreCase);
        if (srcIndex < 0)
            return null;

        var relativePath = normalizedPath.Substring(srcIndex + "/src/".Length);
        var fileNameIndex = relativePath.LastIndexOf('/');
        if (fileNameIndex < 0)
            return "OffceOs";

        var relativeDirectory = relativePath.Substring(0, fileNameIndex);
        if (relativeDirectory.Length == 0)
            return "OffceOs";

        return "OffceOs." + relativeDirectory.Replace('/', '.');
    }
}
