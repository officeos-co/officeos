namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
public class OrganizationsMutations
{
    private static async Task InvalidateOrgCacheAsync(
        IDistributedCache cache,
        UserContext user,
        CancellationToken ct)
    {
        await cache.RemoveAsync($"org:dashboard:{user.Id}", ct);
    }

    [GraphQLDescription("Invites a user to the organization by email. Only the org owner can invite.")]
    public async Task<OrgMemberRecord> InviteMember(
        InviteMemberInput input,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var member = await orgService.InviteMemberAsync(user.Id, user.Email, user.Name, input.Email, input.Role, ct);
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
            await cache.RemoveAsync($"org:dashboard:{user.Id}", ct);
            await cache.RemoveAsync($"auth:me:{user.Id}", ct);
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

    [GraphQLDescription("Removes a member from the organization. Only the org owner can remove.")]
    public async Task<bool> RemoveMember(
        Guid memberId,
        [Service] UserContext user,
        [Service] IOrganizationService orgService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var result = await orgService.RemoveMemberAsync(user.Id, user.Email, user.Name, memberId, ct);
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
            var renamed = await orgService.RenameAsync(user.Id, user.Email, user.Name, input.Name, ct);
            var members = await orgService.ListMembersAsync(user.Id, user.Email, user.Name, ct);
            var result = new OrganizationPayload(renamed.Id, renamed.Name, renamed.OwnerUserId, renamed.CreatedAt, members);
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
