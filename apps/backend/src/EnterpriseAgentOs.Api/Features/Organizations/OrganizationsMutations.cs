namespace EnterpriseAgentOs.Api.Features.Organizations;

[ExtendObjectType(typeof(GraphQLMutations))]
public class OrganizationsMutations
{
    private static void InvalidateOrgCache(IMemoryCache cache, IResolverContext context)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        cache.Remove($"org:dashboard:{user.Id}");
    }

    public async Task<OrgMemberRecord> InviteMember(
        InviteMemberInput input,
        IResolverContext context,
        [Service] IOrganizationService orgService,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        try
        {
            var member = await orgService.InviteMemberAsync(user.Id, user.Email, user.Name, input.Email, input.Role, ct);
            InvalidateOrgCache(cache, context);
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

    public async Task<bool> RemoveMember(
        Guid memberId,
        IResolverContext context,
        [Service] IOrganizationService orgService,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        try
        {
            var result = await orgService.RemoveMemberAsync(user.Id, user.Email, user.Name, memberId, ct);
            InvalidateOrgCache(cache, context);
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

    public async Task<OrganizationPayload> RenameOrg(
        RenameOrgInput input,
        IResolverContext context,
        [Service] IOrganizationService orgService,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        try
        {
            var renamed = await orgService.RenameAsync(user.Id, user.Email, user.Name, input.Name, ct);
            var members = await orgService.ListMembersAsync(user.Id, user.Email, user.Name, ct);
            var result = new OrganizationPayload(renamed.Id, renamed.Name, renamed.OwnerUserId, renamed.CreatedAt, members);
            InvalidateOrgCache(cache, context);
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
