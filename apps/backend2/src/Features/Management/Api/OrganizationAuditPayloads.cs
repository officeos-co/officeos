namespace OffceOs.Api.Features.Management;

public sealed record OrganizationAuditLogFilterInput(
    Guid OrganizationId,
    DateTime? From,
    DateTime? To,
    string? Action,
    Guid? ActorUserId,
    Guid? WorkspaceId,
    Guid? AgentId,
    string? Outcome,
    string? Search,
    int? Limit);

public sealed record OrganizationAuditLogPayload(
    Guid Id,
    Guid OrganizationId,
    Guid? ActorUserId,
    Guid? WorkspaceId,
    Guid? AgentId,
    string Action,
    string ResourceType,
    string? ResourceId,
    string Outcome,
    string? CorrelationId,
    string MetadataJson,
    DateTime OccurredAt);

public sealed record OrganizationAuditExportPayload(
    string Content,
    string ContentType,
    string FileName);

internal static class OrganizationAuditGraphQLMapper
{
    public static OrganizationAuditLogFilter ToFilter(OrganizationAuditLogFilterInput input) => new()
    {
        OrganizationId = input.OrganizationId,
        From = input.From,
        To = input.To,
        Action = input.Action,
        ActorUserId = input.ActorUserId,
        WorkspaceId = input.WorkspaceId,
        AgentId = input.AgentId,
        Outcome = input.Outcome,
        Search = input.Search,
        Limit = input.Limit ?? 100,
    };

    public static OrganizationAuditLogPayload ToPayload(OrganizationAuditLogRecord record) => new(
        record.Id,
        record.OrganizationId,
        record.ActorUserId,
        record.WorkspaceId,
        record.AgentId,
        record.Action,
        record.ResourceType,
        record.ResourceId,
        record.Outcome,
        record.CorrelationId,
        record.MetadataJson,
        record.OccurredAt);

    public static OrganizationAuditExportPayload ToPayload(OrganizationAuditExportResult result) => new(
        result.Content,
        result.ContentType,
        result.FileName);
}
