namespace OffceOs.Domain.Features.Management;

public sealed record InvoiceRecord(
    string Id,
    DateTime Date,
    string Total,
    string Currency,
    string Status,
    string? HostedUrl,
    string? PdfUrl);
