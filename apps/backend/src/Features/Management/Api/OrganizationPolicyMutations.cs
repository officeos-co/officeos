namespace OffceOs.Api.Features.Management;

[ExtendObjectType(typeof(GraphQLMutations))]
public sealed class OrganizationPolicyMutations
{
    public async Task<OrganizationPolicyProfilePayload> UpdateOrganizationPolicyProfile(
        UpdateOrganizationPolicyProfileInput input,
        [Service] UserContext user,
        [Service] IOrganizationPolicyService organizationPolicyService,
        CancellationToken ct)
    {
        try
        {
            var profile = await organizationPolicyService.UpdateAsync(
                user.Id,
                OrganizationPolicyGraphQLMapper.ToRecord(input),
                ct);
            return OrganizationPolicyGraphQLMapper.ToPayload(profile);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }
}
