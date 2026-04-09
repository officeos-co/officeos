using System.ComponentModel.DataAnnotations;

namespace EnterpriseAgentOs.Api.Entities.Agents.Models;

public sealed record CreateAgentRequest(
    [Required, MinLength(1)] string Name,
    string? Model);
