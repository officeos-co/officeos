namespace EnterpriseAgentOs.Api.GraphQL;

public sealed record WhatsAppConnectionStatusPayload(
    Guid ConnectionId,
    string Status,
    string? QrData);
