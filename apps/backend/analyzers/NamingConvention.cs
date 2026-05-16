using Microsoft.CodeAnalysis;

namespace OffceOs.Architecture.Analyzers;

internal static class NamingConvention
{
    private static readonly string[] DomainSuffixes =
    [
        "Record",
        "Filter",
        "Event",
        "Result",
        "Request",
        "Response",
        "Config",
        "Message",
        "Context",
        "Definition",
        "Step",
        "Provider",
        "Registry",
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
        "Extensions",
        "Expression",
        "Usage",
        "Composer",
    ];

    private static readonly string[] DomainValueObjectNames =
    [
        "Email",
        "PersonalityFileName",
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

    private static readonly string[] DependencyFieldSuffixes =
    [
        "Repository",
        "Service",
        "Gateway",
        "Protector",
        "Publisher",
        "Client",
        "Dispatcher",
        "Store",
        "Sandbox",
        "Deployer",
        "Cleaner",
        "Guard",
        "Factory",
        "Config",
        "Resolver",
        "Builder",
        "Executor",
        "Lifecycle",
        "Checkpoint",
        "Loop",
        "Parser",
        "Detector",
        "Binder",
        "Context",
        "Runtime",
        "Registry",
    ];

    private static readonly string[] IgnoredDependencyFieldTypeNames =
    [
        "List",
        "Dictionary",
        "HashSet",
        "ConcurrentDictionary",
        "IReadOnlyList",
        "IEnumerable",
        "Func",
        "Lazy",
        "IDisposable",
        "IAsyncDisposable",
        "MemoryStream",
        "StreamReader",
        "Guid",
        "String",
        "Boolean",
        "Object",
    ];

    public static bool IsAllowedLayerName(INamedTypeSymbol typeSymbol, string layer, string name)
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

        return EndsWithAny(name, suffixes)
            || layer == "Domain" && DomainValueObjectNames.Contains(name);
    }

    public static bool EndsWithAny(string name, string[] suffixes) =>
        suffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal));

    public static string MatchingSuffix(string name, string[] suffixes) =>
        suffixes.First(suffix => name.EndsWith(suffix, StringComparison.Ordinal));

    public static string? GetExpectedDependencyFieldName(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
            return null;

        var typeName = namedTypeSymbol.Name;
        var exactName = typeName switch
        {
            "ILogger" => "logger",
            "IPublisher" => "publisher",
            "IDistributedCache" => "distributedCache",
            "IDataProtector" => "dataProtector",
            "IHttpContextAccessor" => "httpContextAccessor",
            "IHttpClientFactory" => "httpClientFactory",
            "ITopicEventSender" => "topicEventSender",
            "IHostEnvironment" => "hostEnvironment",
            "IServiceScopeFactory" => "serviceScopeFactory",
            "RequestDelegate" => "requestDelegate",
            "FieldDelegate" => "fieldDelegate",
            "HttpClient" => "httpClient",
            "EaosDbContext" => "eaosDbContext",
            "IKubernetes" => "kubernetes",
            "IAmazonS3" => "amazonS3",
            _ => null,
        };

        if (exactName is not null)
            return "_" + exactName;

        if (IgnoredDependencyFieldTypeNames.Contains(typeName))
            return null;

        var dependencyTypeName = StripInterfacePrefix(typeName);
        if (!EndsWithAny(dependencyTypeName, DependencyFieldSuffixes))
            return null;

        return "_" + ToCamelCase(dependencyTypeName);
    }

    private static bool StartsWithIAndEndsWith(string name, string[] suffixes) =>
        name.StartsWith("I", StringComparison.Ordinal) && EndsWithAny(name, suffixes);

    private static string StripInterfacePrefix(string typeName) =>
        typeName.Length > 2 && typeName[0] == 'I' && char.IsUpper(typeName[1])
            ? typeName.Substring(1)
            : typeName;

    private static string ToCamelCase(string typeName)
    {
        if (typeName.StartsWith("OAuth", StringComparison.Ordinal))
            return "oauth" + typeName.Substring(5);

        return char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);
    }
}
