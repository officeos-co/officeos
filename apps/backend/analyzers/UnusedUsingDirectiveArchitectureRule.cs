using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

internal sealed class UnusedUsingDirectiveArchitectureRule : IArchitectureRule
{
    public void Register(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(startContext =>
        {
            startContext.RegisterSyntaxTreeAction(treeContext =>
                AnalyzeSyntaxTree(treeContext, startContext.Compilation));
        });
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, Compilation compilation)
    {
        var filePath = ArchitecturePaths.Normalize(context.Tree.FilePath);
        if (!ArchitecturePaths.IsBackendSourceFile(filePath)
            && !ArchitecturePaths.IsBackendTestFile(filePath))
        {
            return;
        }

        if (ArchitecturePaths.IsDatabaseMigrationFile(filePath))
            return;

        var root = context.Tree.GetRoot(context.CancellationToken);
        var usingDirectives = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(usingDirective =>
                usingDirective.GlobalKeyword == default
                && usingDirective.StaticKeyword == default
                && usingDirective.Alias is null
                && usingDirective.Name is not null)
            .ToList();
        if (usingDirectives.Count == 0)
            return;

        var semanticModel = compilation.GetSemanticModel(context.Tree);
        var declaredNamespace = GetDeclaredNamespace(root);
        var usedNamespaces = CollectUsedNamespaces(root, semanticModel, context.CancellationToken);
        var seenImports = new HashSet<string>(StringComparer.Ordinal);

        foreach (var usingDirective in usingDirectives)
        {
            var namespaceName = usingDirective.Name!.ToString();
            if (!seenImports.Add(namespaceName)
                || string.Equals(namespaceName, declaredNamespace, StringComparison.Ordinal)
                || !usedNamespaces.Contains(namespaceName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ArchitectureDiagnostics.UnusedUsingDirectiveRule,
                    usingDirective.GetLocation(),
                    namespaceName));
            }
        }
    }

    private static HashSet<string> CollectUsedNamespaces(
        SyntaxNode root,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var usedNamespaces = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsInsideUsingDirective(node))
                continue;

            switch (node)
            {
                case AttributeSyntax attribute:
                    AddSymbolNamespace(semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol, usedNamespaces);
                    break;

                case IdentifierNameSyntax identifierName:
                    if (!IsQualifiedNamePart(identifierName))
                        AddSymbolNamespace(semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol, usedNamespaces);
                    break;

                case GenericNameSyntax genericName:
                    if (!IsQualifiedNamePart(genericName))
                        AddSymbolNamespace(semanticModel.GetSymbolInfo(genericName, cancellationToken).Symbol, usedNamespaces);
                    break;
            }
        }

        return usedNamespaces;
    }

    private static void AddSymbolNamespace(ISymbol? symbol, HashSet<string> usedNamespaces)
    {
        if (symbol is null)
            return;

        var namespaceSymbol = symbol switch
        {
            IMethodSymbol { ReducedFrom: { } reducedFrom } => reducedFrom.ContainingNamespace,
            IMethodSymbol methodSymbol => methodSymbol.ContainingType?.ContainingNamespace,
            IPropertySymbol propertySymbol => propertySymbol.ContainingType?.ContainingNamespace,
            IFieldSymbol fieldSymbol => fieldSymbol.ContainingType?.ContainingNamespace,
            IEventSymbol eventSymbol => eventSymbol.ContainingType?.ContainingNamespace,
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.ContainingNamespace,
            _ => symbol.ContainingNamespace,
        };

        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
            return;

        usedNamespaces.Add(namespaceSymbol.ToDisplayString());
    }

    private static bool IsInsideUsingDirective(SyntaxNode node) =>
        node.AncestorsAndSelf().Any(ancestor => ancestor is UsingDirectiveSyntax);

    private static bool IsQualifiedNamePart(SimpleNameSyntax name) =>
        name.Parent is QualifiedNameSyntax or AliasQualifiedNameSyntax;

    private static string? GetDeclaredNamespace(SyntaxNode root)
    {
        var fileScoped = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScoped is not null)
            return fileScoped.Name.ToString();

        return root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
    }
}
