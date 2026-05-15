namespace OffceOs.Domain.Features.AgentHarness;

public sealed record MessageReceivedEvent(
    Guid AgentId,
    string Content,
    string CorrelationId,
    string Purpose = AgentWorkPurposeKinds.Manual,
    Guid? DefinitionId = null) : DomainEvent;
