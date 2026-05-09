using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

internal sealed class NamedTypeArchitectureRule : IArchitectureRule
{
    public void Register(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        var declaringSyntax = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (declaringSyntax is null)
            return;

        var filePath = ArchitecturePaths.Normalize(declaringSyntax.SyntaxTree.FilePath);
        if (!ArchitecturePaths.IsFeatureFile(filePath))
            return;

        var location = declaringSyntax.GetSyntax(context.CancellationToken).GetLocation();
        if (typeSymbol.Name.EndsWith("Dto", StringComparison.Ordinal))
        {
            if (ArchitecturePaths.IsDomainFile(filePath))
            {
                context.ReportDiagnostic(Diagnostic.Create(ArchitectureDiagnostics.DomainDtoTypeRule, location, typeSymbol.Name));
                return;
            }

            if (ArchitecturePaths.IsInLayer(filePath, "Api") || ArchitecturePaths.IsInLayer(filePath, "Application"))
                context.ReportDiagnostic(Diagnostic.Create(ArchitectureDiagnostics.FeatureDtoTypeRule, location, typeSymbol.Name));
        }

        AnalyzeLayerNaming(context, typeSymbol, filePath, location);
        AnalyzeLayerPlacement(context, typeSymbol, filePath, location);
    }

    private static void AnalyzeLayerNaming(
        SymbolAnalysisContext context,
        INamedTypeSymbol typeSymbol,
        string filePath,
        Location location)
    {
        if (typeSymbol.DeclaredAccessibility is Accessibility.Private)
            return;

        if (typeSymbol.TypeKind is TypeKind.Enum)
            return;

        var name = typeSymbol.Name;
        if (name.StartsWith("<", StringComparison.Ordinal))
            return;

        var layer = ArchitecturePaths.GetLayer(filePath);
        if (layer is null)
            return;

        if (NamingConvention.IsAllowedLayerName(typeSymbol, layer, name))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ArchitectureDiagnostics.LayerNamingRule,
            location,
            typeSymbol.TypeKind.ToString().ToLowerInvariant(),
            name,
            layer));
    }

    private static void AnalyzeLayerPlacement(
        SymbolAnalysisContext context,
        INamedTypeSymbol typeSymbol,
        string filePath,
        Location location)
    {
        if (typeSymbol.DeclaredAccessibility is Accessibility.Private)
            return;

        var layer = ArchitecturePaths.GetLayer(filePath);
        if (layer is null)
            return;

        var name = typeSymbol.Name;
        if (name.StartsWith("<", StringComparison.Ordinal))
            return;

        if (name.EndsWith("Entity", StringComparison.Ordinal))
        {
            ReportPlacement(context, location, name, "Entity", "src/Database/Models", layer);
            return;
        }

        if (name.EndsWith("Repository", StringComparison.Ordinal))
        {
            var expected = typeSymbol.TypeKind is TypeKind.Interface ? "Domain" : "Infrastructure";
            if (layer != expected)
                ReportPlacement(context, location, name, "Repository", expected, layer);
            return;
        }

        if (NamingConvention.EndsWithAny(name, ["Record", "Filter", "Event"]))
        {
            var suffix = NamingConvention.MatchingSuffix(name, ["Record", "Filter", "Event"]);
            if (layer != "Domain")
                ReportPlacement(context, location, name, suffix, "Domain", layer);
            return;
        }

        if (NamingConvention.EndsWithAny(name, ["Input", "Payload", "Queries", "Mutations", "Subscriptions", "Controller", "Endpoint"]))
        {
            var suffix = NamingConvention.MatchingSuffix(name, ["Input", "Payload", "Queries", "Mutations", "Subscriptions", "Controller", "Endpoint"]);
            if (layer != "Api")
                ReportPlacement(context, location, name, suffix, "Api", layer);
            return;
        }

        if (NamingConvention.EndsWithAny(name, ["Projection", "Export", "Policy"]))
        {
            var suffix = NamingConvention.MatchingSuffix(name, ["Projection", "Export", "Policy"]);
            if (layer != "Application")
                ReportPlacement(context, location, name, suffix, "Application", layer);
            return;
        }

        if (NamingConvention.EndsWithAny(name, ["Request", "Result"]))
        {
            var suffix = NamingConvention.MatchingSuffix(name, ["Request", "Result"]);
            if (layer is not ("Application" or "Domain"))
                ReportPlacement(context, location, name, suffix, "Application or Domain", layer);
            return;
        }

        if (name.EndsWith("Handler", StringComparison.Ordinal) && layer != "EventHandlers")
            ReportPlacement(context, location, name, "Handler", "EventHandlers", layer);
    }

    private static void ReportPlacement(
        SymbolAnalysisContext context,
        Location location,
        string typeName,
        string suffix,
        string expectedLayer,
        string actualLayer)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            ArchitectureDiagnostics.LayerPlacementRule,
            location,
            typeName,
            suffix,
            expectedLayer,
            actualLayer));
    }
}
