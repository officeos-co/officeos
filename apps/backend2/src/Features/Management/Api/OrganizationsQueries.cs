namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLQueries))]
public class OrganizationsQueries
{
    [GraphQLDescription("Returns the active organization with member list. Creates the user's individual organization when missing.")]
    public async Task<OrganizationPayload> Org(
        [Service] UserContext user,
        [Service] IOrganizationService orgs,
        CancellationToken ct)
    {
        var overview = await orgs.GetCurrentOverviewAsync(user.Id, ct);
        return OrganizationGraphQLMapper.ToPayload(overview);
    }

    [GraphQLDescription("Returns owned, joined, pending, and selected organization context for the authenticated user.")]
    public async Task<OrganizationContextPayload> OrganizationContext(
        [Service] UserContext user,
        [Service] IOrganizationService orgs,
        CancellationToken ct)
    {
        await orgs.EnsureOrganizationAsync(user.Id, user.Email, user.Name, ct);
        var owned = await orgs.GetOwnedOrganizationAsync(user.Id, ct);
        var joined = await orgs.ListJoinedOrganizationsAsync(user.Id, ct);
        var pending = await orgs.ListPendingInvitesAsync(user.Id, user.Email, ct);
        var overview = await orgs.GetCurrentOverviewAsync(user.Id, ct);

        return new OrganizationContextPayload(
            OrganizationGraphQLMapper.ToPayload(overview),
            owned is null ? null : OrganizationGraphQLMapper.ToSummaryPayload(owned),
            joined.Select(OrganizationGraphQLMapper.ToSummaryPayload).ToList(),
            pending.Select(ToPayload).ToList());
    }

    [GraphQLDescription("Returns the organization owned by the authenticated user, if any.")]
    public async Task<OrganizationSummaryPayload?> OwnedOrganization(
        [Service] UserContext user,
        [Service] IOrganizationService orgs,
        CancellationToken ct)
    {
        var owned = await orgs.GetOwnedOrganizationAsync(user.Id, ct);
        return owned is null ? null : OrganizationGraphQLMapper.ToSummaryPayload(owned);
    }

    [GraphQLDescription("Returns organizations where the authenticated user is an active member.")]
    public async Task<IReadOnlyList<OrganizationSummaryPayload>> JoinedOrganizations(
        [Service] UserContext user,
        [Service] IOrganizationService orgs,
        CancellationToken ct)
    {
        var joined = await orgs.ListJoinedOrganizationsAsync(user.Id, ct);
        return joined.Select(OrganizationGraphQLMapper.ToSummaryPayload).ToList();
    }

    [GraphQLDescription("Returns the active organization context.")]
    public async Task<OrganizationSummaryPayload> CurrentOrganizationContext(
        [Service] UserContext user,
        [Service] IOrganizationService orgs,
        CancellationToken ct)
    {
        var current = await orgs.GetCurrentOrganizationAsync(user.Id, ct);
        return OrganizationGraphQLMapper.ToSummaryPayload(current);
    }

    [GraphQLDescription("Returns pending organization invitations for the authenticated user's email.")]
    public async Task<IReadOnlyList<OrganizationInvitePayload>> PendingOrganizationInvites(
        [Service] UserContext user,
        [Service] IOrganizationService orgs,
        CancellationToken ct)
    {
        var invites = await orgs.ListPendingInvitesAsync(user.Id, user.Email, ct);
        return invites.Select(ToPayload).ToList();
    }

    private static OrganizationInvitePayload ToPayload(OrganizationInviteRecord invite)
        => new(
            invite.Id,
            invite.OrganizationId,
            invite.OrganizationName,
            invite.Email,
            invite.Role.ToString(),
            invite.CreatedAt);
}
