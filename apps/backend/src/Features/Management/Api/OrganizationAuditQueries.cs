namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public sealed class OrganizationAuditQueries
{
    public async Task<IReadOnlyList<OrganizationAuditLogPayload>> GetOrganizationAuditLogs(
        OrganizationAuditLogFilterInput input,
        [Service] UserContext user,
        [Service] IOrganizationAuditLogService organizationAuditLogService,
        CancellationToken ct)
    {
        try
        {
            var records = await organizationAuditLogService.ListAsync(
                user.Id,
                OrganizationAuditGraphQLMapper.ToFilter(input),
                ct);
            return records.Select(OrganizationAuditGraphQLMapper.ToPayload).ToList();
        }
        catch (InvalidOperationException ex)
        {
            throw Forbidden(ex.Message);
        }
    }

    public async Task<OrganizationAuditExportPayload> ExportOrganizationAuditLogs(
        OrganizationAuditLogFilterInput input,
        string format,
        [Service] UserContext user,
        [Service] IOrganizationAuditLogService organizationAuditLogService,
        CancellationToken ct)
    {
        try
        {
            var result = await organizationAuditLogService.ExportAsync(
                user.Id,
                OrganizationAuditGraphQLMapper.ToFilter(input),
                format,
                ct);
            return OrganizationAuditGraphQLMapper.ToPayload(result);
        }
        catch (InvalidOperationException ex)
        {
            throw Forbidden(ex.Message);
        }
    }

    private static GraphQLException Forbidden(string message) =>
        new(ErrorBuilder.New().SetMessage(message).SetCode("FORBIDDEN").Build());
}
