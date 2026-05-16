using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace OffceOs.Architecture.Analyzers;

internal static class ArchitectureDiagnostics
{
    private const string Category = "OffceOs.Architecture";

    public static readonly DiagnosticDescriptor DomainDtoTypeRule = new(
        "EAOS001",
        "Domain must not define DTO types",
        "Domain type '{0}' uses the DTO suffix; use a business name such as *Record, *Filter, or a stable domain contract name",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BroadTypesFileRule = new(
        "EAOS002",
        "Feature layers must not use broad Types files",
        "Feature file '{0}' uses the broad *Types.cs naming pattern; use a specific name such as *Input, *Payload, *Record, *Projection, or *Contracts",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ApiMutationRepositoryInjectionRule = new(
        "EAOS003",
        "API mutations must not inject repositories",
        "API mutation parameter '{0}' injects repository '{1}'; call an Application service instead",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DomainForbiddenDependencyRule = new(
        "EAOS004",
        "Domain must not depend on outer layers or transport/infrastructure frameworks",
        "Domain file references forbidden namespace '{0}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FeatureDtoTypeRule = new(
        "EAOS005",
        "Feature API/Application layers must not define DTO types",
        "Feature type '{0}' uses the DTO suffix; use API *Input/*Payload or Application *Request/*Result/*Projection naming",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LayerNamingRule = new(
        "EAOS006",
        "Feature type name must match its layer vocabulary",
        "{0} type '{1}' does not match the allowed naming vocabulary for the {2} layer",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LayerPlacementRule = new(
        "EAOS007",
        "Feature type suffix must be declared in the correct layer",
        "Type '{0}' uses suffix '{1}', which belongs under {2}, not {3}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ApiBoundaryRequestRule = new(
        "EAOS008",
        "API boundary must not expose Application request/result types",
        "API parameter '{0}' exposes '{1}'; define an Api *Input type and map it to the Application request inside the method",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GlobalUsingOnlyRule = new(
        "EAOS009",
        "OffceOs namespaces must not be global imports",
        "Global using directive '{0}' must be moved to the files that need it",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DependencyFieldNamingRule = new(
        "EAOS010",
        "Dependency field name must match its type name",
        "Dependency field '{0}' for type '{1}' must be named '{2}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ApplicationInterfaceFileRule = new(
        "EAOS011",
        "Application service interfaces must live in dedicated contract files",
        "Application interface '{0}' must be declared in '{1}', not implementation file '{2}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly ImmutableArray<DiagnosticDescriptor> All = ImmutableArray.Create(
        DomainDtoTypeRule,
        BroadTypesFileRule,
        ApiMutationRepositoryInjectionRule,
        DomainForbiddenDependencyRule,
        FeatureDtoTypeRule,
        LayerNamingRule,
        LayerPlacementRule,
        ApiBoundaryRequestRule,
        GlobalUsingOnlyRule,
        DependencyFieldNamingRule,
        ApplicationInterfaceFileRule);
}
