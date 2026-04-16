namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class OrganizationsMutations
{
    public async Task<EnterpriseAgentOs.Api.Entities.Organizations.Types.OrgMemberPayload> InviteMember(
        EnterpriseAgentOs.Api.Entities.Organizations.Types.InviteMemberInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Organizations.IOrganizationRepository orgs,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
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
        return new EnterpriseAgentOs.Api.Entities.Organizations.Types.OrgMemberPayload(
            member.Id, member.OrganizationId, member.UserId, member.Email, null, member.Role, member.Status, member.CreatedAt);
    }

    public async Task<bool> RemoveMember(
        Guid memberId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Organizations.IOrganizationRepository orgs,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
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
        return await orgs.RemoveMemberAsync(memberId, ct);
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Organizations.Types.OrganizationPayload> RenameOrg(
        EnterpriseAgentOs.Api.Entities.Organizations.Types.RenameOrgInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Organizations.IOrganizationRepository orgs,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
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
        return new EnterpriseAgentOs.Api.Entities.Organizations.Types.OrganizationPayload(
            renamed.Id, renamed.Name, renamed.OwnerUserId, renamed.CreatedAt,
            members.Select(m => new EnterpriseAgentOs.Api.Entities.Organizations.Types.OrgMemberPayload(
                m.Id, m.OrganizationId, m.UserId, m.Email, null, m.Role, m.Status, m.CreatedAt)).ToList());
    }
}
