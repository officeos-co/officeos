namespace EnterpriseAgentOs.Api.GraphQL.Types;

public sealed record WhatsAppConnectionStatusPayload(
    Guid ConnectionId,
    string Status,
    string? QrData);
