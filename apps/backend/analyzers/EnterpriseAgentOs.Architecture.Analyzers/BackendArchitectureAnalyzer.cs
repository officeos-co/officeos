using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EnterpriseAgentOs.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BackendArchitectureAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DomainDtoTypeRule = new(
        "EAOS001",
        "Domain must not define DTO types",
        "Domain type '{0}' uses the DTO suffix; use a business name such as *Record, *Filter, or a stable domain contract name",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor BroadTypesFileRule = new(
        "EAOS002",
        "Feature layers must not use broad Types files",
        "Feature file '{0}' uses the broad *Types.cs naming pattern; use a specific name such as *Input, *Payload, *Record, *Projection, or *Contracts",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ApiMutationRepositoryInjectionRule = new(
        "EAOS003",
        "API mutations must not inject repositories",
        "API mutation parameter '{0}' injects repository '{1}'; call an Application service instead",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DomainForbiddenDependencyRule = new(
        "EAOS004",
        "Domain must not depend on outer layers or transport/infrastructure frameworks",
        "Domain file references forbidden namespace '{0}'",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FeatureDtoTypeRule = new(
        "EAOS005",
        "Feature API/Application layers must not define DTO types",
        "Feature type '{0}' uses the DTO suffix; use API *Input/*Payload or Application *Request/*Result/*Projection naming",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor LayerNamingRule = new(
        "EAOS006",
        "Feature type name must match its layer vocabulary",
        "{0} type '{1}' does not match the allowed naming vocabulary for the {2} layer",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly string[] ForbiddenDomainNamespaces =
    [
        "EnterpriseAgentOs.Api",
        "EnterpriseAgentOs.Infrastructure",
        "EnterpriseAgentOs.Database",
        "Microsoft.EntityFrameworkCore",
        "HotChocolate",
        "Microsoft.AspNetCore",
        "KubernetesClient",
        "Stripe",
    ];

    private static readonly string[] DomainSuffixes =
    [
        "Record",
        "Filter",
        "Event",
        "Result",
        "Request",
        "Response",
        "Payload",
        "Config",
        "Message",
        "Context",
        "Definition",
        "Step",
        "Provider",
        "Access",
        "Limits",
        "Hasher",
        "Topics",
        "Key",
        "Mode",
        "Modes",
        "Kinds",
        "Kind",
        "State",
        "Descriptor",
        "Tool",
        "Deployment",
        "Row",
        "Options",
        "Page",
        "Overview",
        "Exception",
        "Subscription",
        "Limit",
    ];

    private static readonly string[] DomainInterfaceSuffixes =
    [
        "Repository",
        "Service",
        "Gateway",
        "Client",
        "Manager",
        "Sandbox",
        "Deployer",
        "Cleaner",
        "Guard",
    ];

    private static readonly string[] ApplicationSuffixes =
    [
        "Service",
        "Request",
        "Result",
        "Policy",
        "Input",
        "Payload",
        "Projection",
        "Entry",
        "Item",
        "Export",
        "Context",
        "Builder",
        "Executor",
        "Publisher",
        "Resolver",
        "Parser",
        "Detector",
        "Checkpoint",
        "Guard",
        "History",
        "Lifecycle",
        "Scope",
        "Loop",
        "Session",
        "Connection",
        "Shell",
        "Init",
        "Bootstrap",
        "Summary",
        "Point",
        "Breakdown",
        "Page",
        "Mapper",
        "Usage",
        "Binder",
        "Client",
        "Tool",
        "Registry",
        "Factory",
        "Store",
        "Keys",
        "Message",
        "Call",
        "Window",
        "Schema",
        "Kind",
    ];

    private static readonly string[] ApiSuffixes =
    [
        "Input",
        "Payload",
        "Request",
        "Queries",
        "Mutations",
        "Subscriptions",
        "Controller",
        "Endpoint",
        "Mapper",
        "Bootstrap",
        "Summary",
    ];

    private static readonly string[] InfrastructureSuffixes =
    [
        "Repository",
        "Adapter",
        "Client",
        "Gateway",
        "Config",
        "Protector",
        "Dispatcher",
        "Translator",
        "Sandbox",
        "Store",
        "Injector",
        "Router",
        "Service",
        "Manager",
        "Handle",
        "Response",
    ];

    private static readonly string[] EventHandlersSuffixes = ["Handler"];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DomainDtoTypeRule,
            BroadTypesFileRule,
            ApiMutationRepositoryInjectionRule,
            DomainForbiddenDependencyRule,
            FeatureDtoTypeRule,
            LayerNamingRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxTreeAction(AnalyzeFileName);
        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    private static void AnalyzeFileName(SyntaxTreeAnalysisContext context)
    {
        var filePath = NormalizePath(context.Tree.FilePath);
        if (!IsFeatureFile(filePath))
            return;

        var fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith("Types.cs", StringComparison.Ordinal))
            ReportFileDiagnostic(context, BroadTypesFileRule, fileName);

        if (IsDomainFile(filePath) && fileName.EndsWith("Dto.cs", StringComparison.Ordinal))
            ReportFileDiagnostic(context, DomainDtoTypeRule, Path.GetFileNameWithoutExtension(fileName));
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var filePath = NormalizePath(context.Node.SyntaxTree.FilePath);
        if (!IsDomainFile(filePath))
            return;

        var usingDirective = (UsingDirectiveSyntax)context.Node;
        var namespaceName = usingDirective.Name?.ToString();
        if (namespaceName is null)
            return;

        var forbiddenNamespace = ForbiddenDomainNamespaces.FirstOrDefault(forbidden =>
            namespaceName.Equals(forbidden, StringComparison.Ordinal)
            || namespaceName.StartsWith(forbidden + ".", StringComparison.Ordinal));

        if (forbiddenNamespace is null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DomainForbiddenDependencyRule,
            usingDirective.GetLocation(),
            forbiddenNamespace));
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        var declaringSyntax = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (declaringSyntax is null)
            return;

        var filePath = NormalizePath(declaringSyntax.SyntaxTree.FilePath);
        if (!IsFeatureFile(filePath))
            return;

        var location = declaringSyntax.GetSyntax(context.CancellationToken).GetLocation();
        if (typeSymbol.Name.EndsWith("Dto", StringComparison.Ordinal))
        {
            if (IsDomainFile(filePath))
            {
                context.ReportDiagnostic(Diagnostic.Create(DomainDtoTypeRule, location, typeSymbol.Name));
                return;
            }

            if (IsInLayer(filePath, "Api") || IsInLayer(filePath, "Application"))
                context.ReportDiagnostic(Diagnostic.Create(FeatureDtoTypeRule, location, typeSymbol.Name));
        }

        AnalyzeLayerNaming(context, typeSymbol, filePath, location);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var filePath = NormalizePath(context.Node.SyntaxTree.FilePath);
        if (!IsInLayer(filePath, "Api") || !Path.GetFileName(filePath).EndsWith("Mutations.cs", StringComparison.Ordinal))
            return;

        var parameter = (ParameterSyntax)context.Node;
        if (parameter.Type is null)
            return;

        var typeSymbol = context.SemanticModel.GetTypeInfo(parameter.Type, context.CancellationToken).Type;
        if (typeSymbol is null || !IsRepositoryType(typeSymbol))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ApiMutationRepositoryInjectionRule,
            parameter.GetLocation(),
            parameter.Identifier.ValueText,
            typeSymbol.Name));
    }

    private static bool IsRepositoryType(ITypeSymbol typeSymbol)
    {
        var name = typeSymbol.Name;
        if (name.StartsWith("I", StringComparison.Ordinal) && name.EndsWith("Repository", StringComparison.Ordinal))
            return true;

        return typeSymbol.AllInterfaces.Any(interfaceSymbol =>
            interfaceSymbol.Name.StartsWith("I", StringComparison.Ordinal)
            && interfaceSymbol.Name.EndsWith("Repository", StringComparison.Ordinal));
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

        var layer = GetLayer(filePath);
        if (layer is null)
            return;

        if (IsAllowedName(typeSymbol, layer, name))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            LayerNamingRule,
            location,
            typeSymbol.TypeKind.ToString().ToLowerInvariant(),
            name,
            layer));
    }

    private static bool IsAllowedName(INamedTypeSymbol typeSymbol, string layer, string name)
    {
        if (typeSymbol.TypeKind is TypeKind.Interface)
        {
            return layer switch
            {
                "Domain" => StartsWithIAndEndsWith(name, DomainInterfaceSuffixes),
                "Application" => StartsWithIAndEndsWith(name, ApplicationSuffixes),
                "Infrastructure" => StartsWithIAndEndsWith(name, InfrastructureSuffixes),
                _ => name.StartsWith("I", StringComparison.Ordinal),
            };
        }

        var suffixes = layer switch
        {
            "Domain" => DomainSuffixes,
            "Application" => ApplicationSuffixes,
            "Api" => ApiSuffixes,
            "Infrastructure" => InfrastructureSuffixes,
            "EventHandlers" => EventHandlersSuffixes,
            _ => [],
        };

        return EndsWithAny(name, suffixes);
    }

    private static bool StartsWithIAndEndsWith(string name, string[] suffixes) =>
        name.StartsWith("I", StringComparison.Ordinal) && EndsWithAny(name, suffixes);

    private static bool EndsWithAny(string name, string[] suffixes) =>
        suffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal));

    private static string? GetLayer(string filePath)
    {
        if (IsInLayer(filePath, "Domain"))
            return "Domain";
        if (IsInLayer(filePath, "Application"))
            return "Application";
        if (IsInLayer(filePath, "Api"))
            return "Api";
        if (IsInLayer(filePath, "Infrastructure"))
            return "Infrastructure";
        if (IsInLayer(filePath, "EventHandlers"))
            return "EventHandlers";

        return null;
    }

    private static void ReportFileDiagnostic(SyntaxTreeAnalysisContext context, DiagnosticDescriptor descriptor, string argument)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var location = root.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, argument));
    }

    private static bool IsDomainFile(string filePath) =>
        IsInLayer(filePath, "Domain");

    private static bool IsFeatureFile(string filePath) =>
        filePath.Contains("/src/features/");

    private static bool IsInLayer(string filePath, string layer) =>
        filePath.Contains($"/{layer.ToLowerInvariant()}/");

    private static string NormalizePath(string filePath) =>
        filePath.Replace('\\', '/').ToLowerInvariant();
}
