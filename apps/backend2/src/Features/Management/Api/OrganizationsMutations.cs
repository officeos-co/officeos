namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
public class OrganizationsMutations
{
    private static async Task InvalidateOrgCacheAsync(
        IDistributedCache cache,
        UserContext user,
        CancellationToken ct)
    {
        await cache.RemoveAsync($"auth:me:{user.Id}", ct);
    }

    [GraphQLDescription("Sets up the authenticated user's organization by converting the individual organization into a shared organization.")]
    public async Task<OrganizationPayload> CreateOrganization(
        CreateOrganizationInput input,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var created = await orgService.CreateOrganizationAsync(user.Id, user.Email, user.Name, input.Name, ct);
            var members = await orgService.ListMembersAsync(user.Id, ct);
            await InvalidateOrgCacheAsync(cache, user, ct);
            return new OrganizationPayload(created.Id, created.Name, created.Kind.ToStorageString(), created.OwnerUserId, created.CreatedAt, members);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode(ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase) ? "CONFLICT" : "BAD_INPUT")
                .Build());
        }
    }

    [GraphQLDescription("Declines a pending organization invitation for the authenticated user's email.")]
    public async Task<bool> DeclineOrganizationInvite(
        Guid memberId,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var declined = await orgService.DeclineInviteAsync(user.Id, user.Email, memberId, ct);
            await InvalidateOrgCacheAsync(cache, user, ct);
            return declined;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode("BAD_INPUT")
                .Build());
        }
    }

    [GraphQLDescription("Selects the organization context used while the user is in personal workspaces.")]
    public async Task<OrganizationSummaryPayload> SelectOrganization(
        Guid organizationId,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var selected = await orgService.SelectOrganizationAsync(user.Id, organizationId, ct);
            await InvalidateOrgCacheAsync(cache, user, ct);
            return OrganizationGraphQLMapper.ToSummaryPayload(selected);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode("NOT_FOUND")
                .Build());
        }
    }

    [GraphQLDescription("Invites a user to the selected organization by email. Owners and admins can invite.")]
    public async Task<OrgMemberRecord> InviteMember(
        InviteMemberInput input,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var member = await orgService.InviteMemberAsync(user.Id, input.Email, input.Role, ct);
            await InvalidateOrgCacheAsync(cache, user, ct);
            return member;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode(ex.Message.Contains("owner", StringComparison.OrdinalIgnoreCase) ? "FORBIDDEN" : "BAD_INPUT")
                .Build());
        }
    }

    [GraphQLDescription("Accepts a pending organization invitation for the authenticated user's email.")]
    public async Task<OrgMemberRecord> AcceptOrganizationInvite(
        Guid memberId,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var member = await orgService.AcceptInviteAsync(user.Id, user.Email, memberId, ct);
            await InvalidateOrgCacheAsync(cache, user, ct);
            return member;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode("BAD_INPUT")
                .Build());
        }
    }

    [GraphQLDescription("Removes a member from the selected organization. Owners and admins can remove non-owner members.")]
    public async Task<bool> RemoveMember(
        Guid memberId,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var result = await orgService.RemoveMemberAsync(user.Id, memberId, ct);
            await InvalidateOrgCacheAsync(cache, user, ct);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode("FORBIDDEN")
                .Build());
        }
    }

    [GraphQLDescription("Renames the organization. Only the org owner can rename.")]
    public async Task<OrganizationPayload> RenameOrg(
        RenameOrgInput input,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var renamed = await orgService.RenameAsync(user.Id, input.Name, ct);
            var members = await orgService.ListMembersAsync(user.Id, ct);
            var result = new OrganizationPayload(renamed.Id, renamed.Name, renamed.Kind.ToStorageString(), renamed.OwnerUserId, renamed.CreatedAt, members);
            await InvalidateOrgCacheAsync(cache, user, ct);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode(ex.Message.Contains("owner", StringComparison.OrdinalIgnoreCase) ? "FORBIDDEN" : "BAD_INPUT")
                .Build());
        }
    }
}
