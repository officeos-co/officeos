namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record InvoicePayload(
    string Id,
    DateTime Date,
    string Total,
    string Currency,
    string Status,
    string? HostedUrl,
    string? PdfUrl);
