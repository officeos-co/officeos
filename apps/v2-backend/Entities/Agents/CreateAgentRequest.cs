using System.ComponentModel.DataAnnotations;

namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed record CreateAgentRequest(
    [Required, MinLength(1)] string Name,
    string? Model);
