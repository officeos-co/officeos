using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

internal sealed class UsingDirectiveArchitectureRule : IArchitectureRule
{
    private static readonly string[] ForbiddenDomainNamespaces =
    [
        "OffceOs.Api",
        "OffceOs.Infrastructure",
        "OffceOs.Database",
        "Microsoft.EntityFrameworkCore",
        "HotChocolate",
        "Microsoft.AspNetCore",
        "KubernetesClient",
        "Stripe",
    ];

    public void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var filePath = ArchitecturePaths.Normalize(context.Node.SyntaxTree.FilePath);
        var usingDirective = (UsingDirectiveSyntax)context.Node;
        var namespaceName = usingDirective.Name?.ToString();

        if (ArchitecturePaths.IsBackendSourceFile(filePath)
            && usingDirective.GlobalKeyword != default
            && namespaceName is not null
            && IsOffceOsNamespace(namespaceName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ArchitectureDiagnostics.GlobalUsingOnlyRule,
                usingDirective.GetLocation(),
                namespaceName));
        }

        if (!ArchitecturePaths.IsDomainFile(filePath) || namespaceName is null)
            return;

        var forbiddenNamespace = ForbiddenDomainNamespaces.FirstOrDefault(forbidden =>
            namespaceName.Equals(forbidden, StringComparison.Ordinal)
            || namespaceName.StartsWith(forbidden + ".", StringComparison.Ordinal));

        if (forbiddenNamespace is null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ArchitectureDiagnostics.DomainForbiddenDependencyRule,
            usingDirective.GetLocation(),
            forbiddenNamespace));
    }

    private static bool IsOffceOsNamespace(string namespaceName) =>
        namespaceName.Equals("OffceOs", StringComparison.Ordinal)
        || namespaceName.StartsWith("OffceOs.", StringComparison.Ordinal);
}
