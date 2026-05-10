namespace OffceOs.Domain.Features.Billing;

public sealed record InvoiceRecord(
    string Id,
    DateTime Date,
    string Total,
    string Currency,
    string Status,
    string? HostedUrl,
    string? PdfUrl);
