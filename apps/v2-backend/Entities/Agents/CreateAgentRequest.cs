using System.ComponentModel.DataAnnotations;

namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed record CreateAgentRequest(
    [Required, MinLength(1)] string Name,
    [Required, MinLength(1)] string Provider,
    string? Model);
