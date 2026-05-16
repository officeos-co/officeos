using OffceOs.Common.Domain;
using OffceOs.Features.Agents.Domain;
namespace OffceOs.Features.AgentHarness.Domain;

public sealed record MessageReceivedEvent(
    Guid AgentId,
    string Content,
    string CorrelationId,
    string Purpose = AgentWorkPurposeKinds.Manual,
    Guid? DefinitionId = null) : DomainEvent;
