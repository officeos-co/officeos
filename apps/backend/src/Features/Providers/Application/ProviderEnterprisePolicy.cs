namespace OffceOs.Application.Features.Providers;

internal sealed class ProviderEnterprisePolicy
{
    private readonly IOrgSubscriptionRepository _orgSubscriptionRepository;

    public ProviderEnterprisePolicy(IOrgSubscriptionRepository orgSubscriptionRepository)
    {
        _orgSubscriptionRepository = orgSubscriptionRepository;
    }

    public async Task<bool> IsEnterpriseOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var subscription = await _orgSubscriptionRepository.GetByAsync(
            new OrgSubscriptionFilter { OrganizationId = organizationId.ToString() },
            ct);

        return subscription is { Plan: SubscriptionPlan.Enterprise, IsActive: true };
    }

    public async Task RequireEnterpriseOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (!await IsEnterpriseOrganizationAsync(organizationId, ct))
            throw new InvalidOperationException("Enterprise provider profiles require an active enterprise organization subscription.");
    }
}
