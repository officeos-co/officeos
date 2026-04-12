using EnterpriseAgentOs.Api.Entities.Billing;

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
        Assert.Equal(1, PlanLimits.IndividualFree.ConcurrentAgents);
    }

    [Fact]
    public void IndividualFree_Has500KCredits()
    {
        Assert.Equal(500_000L, PlanLimits.IndividualFree.CreditsPerMonth);
    }

    [Fact]
    public void IndividualPro_HasThreeAgents()
    {
        Assert.Equal(3, PlanLimits.IndividualPro.ConcurrentAgents);
    }

    [Fact]
    public void IndividualPro_HasTenMillionCredits()
    {
        Assert.Equal(10_000_000L, PlanLimits.IndividualPro.CreditsPerMonth);
    }

    [Fact]
    public void OrgTeam_HasTenAgents()
    {
        Assert.Equal(10, PlanLimits.OrgTeam.ConcurrentAgents);
    }

    [Fact]
    public void OrgTeam_HasTwentyFiveMillionCredits()
    {
        Assert.Equal(25_000_000L, PlanLimits.OrgTeam.CreditsPerMonth);
    }

    // -------------------------------------------------------------------------
    // ForIndividualPlan — routing
    // -------------------------------------------------------------------------

    [Fact]
    public void ForIndividualPlan_Pro_ReturnsIndividualPro()
    {
        var limit = PlanLimits.ForIndividualPlan("pro");
        Assert.Equal(PlanLimits.IndividualPro, limit);
    }

    [Fact]
    public void ForIndividualPlan_UnknownPlan_ReturnsIndividualFree()
    {
        var limit = PlanLimits.ForIndividualPlan("unknown");
        Assert.Equal(PlanLimits.IndividualFree, limit);
    }

    [Fact]
    public void ForIndividualPlan_EmptyString_ReturnsIndividualFree()
    {
        var limit = PlanLimits.ForIndividualPlan(string.Empty);
        Assert.Equal(PlanLimits.IndividualFree, limit);
    }

    // -------------------------------------------------------------------------
    // ForOrgPlan — routing
    // -------------------------------------------------------------------------

    [Fact]
    public void ForOrgPlan_Team_ReturnsOrgTeam()
    {
        var limit = PlanLimits.ForOrgPlan("team");
        Assert.Equal(PlanLimits.OrgTeam, limit);
    }

    [Fact]
    public void ForOrgPlan_Free_ReturnsOrgFree()
    {
        var limit = PlanLimits.ForOrgPlan("free");
        Assert.Equal(PlanLimits.OrgFree, limit);
    }

    [Fact]
    public void ForOrgPlan_Enterprise_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PlanLimits.ForOrgPlan("enterprise"));
    }
}
