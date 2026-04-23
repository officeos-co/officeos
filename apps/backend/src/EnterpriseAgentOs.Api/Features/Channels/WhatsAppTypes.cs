namespace EnterpriseAgentOs.Api.Features.Channels;

public sealed record WhatsAppConnectionStatusPayload(
    Guid ConnectionId,
    string Status,
    string? QrData);
