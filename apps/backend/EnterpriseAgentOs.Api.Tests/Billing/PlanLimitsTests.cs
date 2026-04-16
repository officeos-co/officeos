namespace EnterpriseAgentOs.Api.Tests.Billing;

/// <summary>
/// Unit tests for PlanLimits — the single source of truth for plan limits.
/// </summary>
public sealed class PlanLimitsTests
{
    // -------------------------------------------------------------------------
    // Static limit values
    // -------------------------------------------------------------------------

    [Fact]
    public void IndividualFree_HasOneAgent()
    {
        Assert.Equal(1, EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualFree.ConcurrentAgents);
    }

    [Fact]
    public void IndividualFree_Has500KCredits()
    {
        Assert.Equal(500_000L, EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualFree.CreditsPerMonth);
    }

    [Fact]
    public void IndividualPro_HasThreeAgents()
    {
        Assert.Equal(3, EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualPro.ConcurrentAgents);
    }

    [Fact]
    public void IndividualPro_HasTenMillionCredits()
    {
        Assert.Equal(10_000_000L, EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualPro.CreditsPerMonth);
    }

    [Fact]
    public void OrgTeam_HasTenAgents()
    {
        Assert.Equal(10, EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.OrgTeam.ConcurrentAgents);
    }

    [Fact]
    public void OrgTeam_HasTwentyFiveMillionCredits()
    {
        Assert.Equal(25_000_000L, EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.OrgTeam.CreditsPerMonth);
    }

    // -------------------------------------------------------------------------
    // ForIndividualPlan — routing
    // -------------------------------------------------------------------------

    [Fact]
    public void ForIndividualPlan_Pro_ReturnsIndividualPro()
    {
        var limit = EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.ForIndividualPlan("pro");
        Assert.Equal(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualPro, limit);
    }

    [Fact]
    public void ForIndividualPlan_UnknownPlan_ReturnsIndividualFree()
    {
        var limit = EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.ForIndividualPlan("unknown");
        Assert.Equal(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualFree, limit);
    }

    [Fact]
    public void ForIndividualPlan_EmptyString_ReturnsIndividualFree()
    {
        var limit = EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.ForIndividualPlan(string.Empty);
        Assert.Equal(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.IndividualFree, limit);
    }

    // -------------------------------------------------------------------------
    // ForOrgPlan — routing
    // -------------------------------------------------------------------------

    [Fact]
    public void ForOrgPlan_Team_ReturnsOrgTeam()
    {
        var limit = EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.ForOrgPlan("team");
        Assert.Equal(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.OrgTeam, limit);
    }

    [Fact]
    public void ForOrgPlan_Free_ReturnsOrgFree()
    {
        var limit = EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.ForOrgPlan("free");
        Assert.Equal(EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.OrgFree, limit);
    }

    [Fact]
    public void ForOrgPlan_Enterprise_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => EnterpriseAgentOs.Api.Entities.Billing.PlanLimits.ForOrgPlan("enterprise"));
    }
}
