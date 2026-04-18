namespace EnterpriseAgentOs.Domain.DTOs.Billing;

public sealed record InvoicePayload(
    string Id,
    DateTime Date,
    string Total,
    string Currency,
    string Status,
    string? HostedUrl,
    string? PdfUrl);
