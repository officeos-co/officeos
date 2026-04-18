namespace EnterpriseAgentOs.Api.Extensions;

public static class GraphQLRegistrationExtensions
{
    /// <summary>
    /// Scans the given assembly for classes annotated with <c>[ExtendObjectType]</c>
    /// and registers each as a type extension on the request executor. This lets
    /// every domain own its own <c>*Queries.cs</c> / <c>*Mutations.cs</c> / <c>*Subscriptions.cs</c>
    /// files and have them auto-register without a central list.
    /// </summary>
    public static HotChocolate.Execution.Configuration.IRequestExecutorBuilder AddDomainTypeExtensions(
        this HotChocolate.Execution.Configuration.IRequestExecutorBuilder builder,
        Assembly assembly)
    {
        var extensions = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.GetCustomAttribute<ExtendObjectTypeAttribute>() is not null);

        foreach (var type in extensions)
        {
            builder.AddTypeExtension(type);
        }

        return builder;
    }
}
