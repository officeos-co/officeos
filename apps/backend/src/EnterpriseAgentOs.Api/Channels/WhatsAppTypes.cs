namespace EnterpriseAgentOs.Api.Channels;

public sealed record WhatsAppConnectionStatusPayload(
    Guid ConnectionId,
    string Status,
    string? QrData);
