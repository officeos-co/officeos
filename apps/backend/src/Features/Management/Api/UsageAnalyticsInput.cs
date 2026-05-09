namespace EnterpriseAgentOs.Api.Features.Management;

public sealed record UsageAnalyticsInput(
    DateTime From,
    DateTime To);
