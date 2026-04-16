namespace EnterpriseAgentOs.Api.Entities.Agents.GraphQL;

public sealed record CreateAgentInput(
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    List<string>? IntegrationSlugs,
    List<string>? ChannelSlugs);

public sealed record UpdateAgentInput(
    string? Name,
    string? Provider,
    string? Model,
    string? Prompt);
