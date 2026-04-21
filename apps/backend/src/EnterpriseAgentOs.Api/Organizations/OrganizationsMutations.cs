namespace EnterpriseAgentOs.Api.Organizations;

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
        [Service] IOrganizationRepository orgs,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var org = await orgs.GetOrCreateDefaultAsync(user.Id, user.Email, user.Name, ct);
        if (org.OwnerUserId != user.Id)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("only the organization owner may invite members")
                .SetCode("FORBIDDEN").Build());
        }
        if (string.IsNullOrWhiteSpace(input.Email) || !input.Email.Contains('@'))
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("valid email required").SetCode("BAD_INPUT").Build());
        }
        var role = input.Role ?? "Member";
        if (role != "Admin" && role != "Member")
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("role must be 'Admin' or 'Member'").SetCode("BAD_INPUT").Build());
        }
        var member = await orgs.AddMemberAsync(org.Id, input.Email.Trim().ToLowerInvariant(), role, "invited", null, ct);
        InvalidateOrgCache(cache, context);
        return member;
    }

    public async Task<bool> RemoveMember(
        Guid memberId,
        IResolverContext context,
        [Service] IOrganizationRepository orgs,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var org = await orgs.GetOrCreateDefaultAsync(user.Id, user.Email, user.Name, ct);
        if (org.OwnerUserId != user.Id)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("only the organization owner may remove members")
                .SetCode("FORBIDDEN").Build());
        }
        var members = await orgs.ListMembersAsync(org.Id, ct);
        var target = members.FirstOrDefault(m => m.Id == memberId);
        if (target is null) return false;
        if (target.Role == "Owner")
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("cannot remove the owner").SetCode("FORBIDDEN").Build());
        }
        var result = await orgs.RemoveMemberAsync(memberId, ct);
        InvalidateOrgCache(cache, context);
        return result;
    }

    public async Task<OrganizationPayload> RenameOrg(
        RenameOrgInput input,
        IResolverContext context,
        [Service] IOrganizationRepository orgs,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var org = await orgs.GetOrCreateDefaultAsync(user.Id, user.Email, user.Name, ct);
        if (org.OwnerUserId != user.Id)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("only the organization owner may rename the org")
                .SetCode("FORBIDDEN").Build());
        }
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("name required").SetCode("BAD_INPUT").Build());
        }
        var renamed = await orgs.RenameAsync(org.Id, input.Name.Trim(), ct);
        var members = await orgs.ListMembersAsync(renamed.Id, ct);
        var result = new OrganizationPayload(
            renamed.Id, renamed.Name, renamed.OwnerUserId, renamed.CreatedAt, members);
        InvalidateOrgCache(cache, context);
        return result;
    }
}
