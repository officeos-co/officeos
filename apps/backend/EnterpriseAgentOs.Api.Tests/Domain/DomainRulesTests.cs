namespace EnterpriseAgentOs.Api.Tests.Domain;

using EnterpriseAgentOs.Domain.Models;
using EnterpriseAgentOs.Domain.DTOs.Billing;

/// <summary>
/// Tests for business rules that belong on Domain models — TDD RED phase.
/// These test domain logic that is being extracted from Application services.
/// </summary>
public sealed class DomainRulesTests
{
    // =========================================================================
    // ModelCostWeights — moved to Domain
    // =========================================================================

    [Theory]
    [InlineData("gpt-4o-mini", 1000, 1000)]       // 1x
    [InlineData("gemini-2.5-flash", 1000, 1000)]   // 1x
    [InlineData("claude-haiku-4-5", 1000, 5000)]   // 5x
    [InlineData("gemini-2.5-pro", 1000, 8000)]     // 8x
    [InlineData("gpt-4o", 1000, 15000)]            // 15x
    [InlineData("claude-sonnet-4-6", 1000, 20000)] // 20x
    [InlineData("claude-opus-4-6", 1000, 75000)]   // 75x
    public void ModelCostWeights_ToCredits_ReturnsCorrectCredits(string model, long rawTokens, long expected)
    {
        Assert.Equal(expected, EnterpriseAgentOs.Domain.Services.ModelCostWeights.ToCredits(model, rawTokens));
    }

    [Fact]
    public void ModelCostWeights_UnknownModel_DefaultsToSonnetWeight()
    {
        // Unknown models default to 20x to avoid undercharging
        Assert.Equal(20_000, EnterpriseAgentOs.Domain.Services.ModelCostWeights.ToCredits("unknown-model", 1000));
    }

    // =========================================================================
    // KnownModels — moved to Domain
    // =========================================================================

    [Theory]
    [InlineData("auto")]
    [InlineData("claude-haiku-4-5")]
    [InlineData("gpt-4o")]
    [InlineData("gemini-2.5-pro")]
    [InlineData("grok-4")]
    public void KnownModels_IsValid_ReturnsTrueForSupportedModels(string model)
    {
        Assert.True(EnterpriseAgentOs.Domain.Services.KnownModels.IsValid(model));
    }

    [Fact]
    public void KnownModels_IsValid_ReturnsFalseForUnknown()
    {
        Assert.False(EnterpriseAgentOs.Domain.Services.KnownModels.IsValid("gpt-3"));
    }

    [Fact]
    public void KnownModels_DefaultModel_IsAuto()
    {
        Assert.Equal("auto", EnterpriseAgentOs.Domain.Services.KnownModels.DefaultModel);
    }

    // =========================================================================
    // ProviderRecord — rich domain model
    // =========================================================================

    [Fact]
    public void ProviderRecord_IsReady_WhenKeyConfigured()
    {
        var provider = new EnterpriseAgentOs.Domain.Models.ProviderRecord { Name = "openai", EncryptedApiKey = "encrypted" };
        Assert.True(provider.IsReady);
    }

    [Fact]
    public void ProviderRecord_IsNotReady_WhenKeyMissing()
    {
        var provider = new EnterpriseAgentOs.Domain.Models.ProviderRecord { Name = "openai" };
        Assert.False(provider.IsReady);
    }

    // =========================================================================
    // UserSubscription — rich domain model
    // =========================================================================

    [Fact]
    public void UserSubscription_CreateDefaultFree_HasCorrectLimits()
    {
        var sub = UserSubscription.CreateDefaultFree(Guid.NewGuid());
        Assert.Equal("free", sub.Plan);
        Assert.Equal(1, sub.ConcurrentAgentLimit);
        Assert.Equal(500_000L, sub.CreditBudgetPerMonth);
        Assert.True(sub.IsActive);
        Assert.Equal("monthly", sub.BillingCycle);
    }

    [Fact]
    public void UserSubscription_CreateDefaultFree_SetsPeriodToFirstOfMonth()
    {
        var sub = UserSubscription.CreateDefaultFree(Guid.NewGuid());
        Assert.Equal(1, sub.PeriodStart.Day);
        Assert.Equal(DateTimeKind.Utc, sub.PeriodStart.Kind);
        Assert.Equal(sub.PeriodStart.AddMonths(1), sub.PeriodEnd);
    }

    [Fact]
    public void UserSubscription_CheckBudget_ReturnsRemainingCredits()
    {
        var sub = UserSubscription.CreateDefaultFree(Guid.NewGuid());
        sub.CreditsUsedThisMonth = 100_000;
        var (remaining, overBudget) = sub.CheckBudget();
        Assert.Equal(400_000L, remaining);
        Assert.False(overBudget);
    }

    [Fact]
    public void UserSubscription_CheckBudget_DetectsOverBudget()
    {
        var sub = UserSubscription.CreateDefaultFree(Guid.NewGuid());
        sub.CreditsUsedThisMonth = 600_000;
        var (remaining, overBudget) = sub.CheckBudget();
        Assert.Equal(-100_000L, remaining);
        Assert.True(overBudget);
    }

    [Fact]
    public void UserSubscription_RecordCredits_IncrementsUsage()
    {
        var sub = UserSubscription.CreateDefaultFree(Guid.NewGuid());
        sub.RecordCredits(50_000);
        Assert.Equal(50_000L, sub.CreditsUsedThisMonth);
        sub.RecordCredits(25_000);
        Assert.Equal(75_000L, sub.CreditsUsedThisMonth);
    }

    // =========================================================================
    // OrgSubscription — rich domain model
    // =========================================================================

    [Fact]
    public void OrgSubscription_CreateDefaultFree_HasCorrectLimits()
    {
        var sub = OrgSubscription.CreateDefaultFree("org-123");
        Assert.Equal("free", sub.Plan);
        Assert.Equal(1, sub.ConcurrentAgentLimit);
        Assert.Equal(500_000L, sub.CreditBudgetPerMonth);
        Assert.True(sub.IsActive);
    }

    [Fact]
    public void OrgSubscription_CreateDefaultFree_SetsPeriodToFirstOfMonth()
    {
        var sub = OrgSubscription.CreateDefaultFree("org-123");
        Assert.Equal(1, sub.PeriodStart.Day);
        Assert.Equal(DateTimeKind.Utc, sub.PeriodStart.Kind);
    }

    [Fact]
    public void OrgSubscription_CheckBudget_ReturnsRemainingCredits()
    {
        var sub = OrgSubscription.CreateDefaultFree("org-123");
        sub.CreditsUsedThisMonth = 300_000;
        var (remaining, overBudget) = sub.CheckBudget();
        Assert.Equal(200_000L, remaining);
        Assert.False(overBudget);
    }

    [Fact]
    public void OrgSubscription_CheckBudget_DetectsOverBudget()
    {
        var sub = OrgSubscription.CreateDefaultFree("org-123");
        sub.CreditsUsedThisMonth = 1_000_000;
        var (remaining, overBudget) = sub.CheckBudget();
        Assert.True(overBudget);
    }

    // =========================================================================
    // AgentRecord — rich domain model
    // =========================================================================

    [Fact]
    public void AgentRecord_StartsAsPending()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic" };
        Assert.Equal("pending", agent.Status);
    }

    [Fact]
    public void AgentRecord_MarkDeployed_SetsRunningAndPodInfo()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic" };
        agent.MarkDeployed("pod-123", "http://pod-123:42617");
        Assert.Equal("running", agent.Status);
        Assert.Equal("pod-123", agent.PodName);
        Assert.Equal("http://pod-123:42617", agent.ServiceUrl);
    }

    [Fact]
    public void AgentRecord_MarkFailed_SetsFailedStatus()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic" };
        agent.MarkFailed();
        Assert.Equal("failed", agent.Status);
    }

    [Fact]
    public void AgentRecord_ValidateModel_AcceptsKnownModel()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic" };
        agent.ValidateAndSetModel("claude-sonnet-4-6"); // should not throw
        Assert.Equal("claude-sonnet-4-6", agent.Model);
    }

    [Fact]
    public void AgentRecord_ValidateModel_DefaultsNullToAuto()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic" };
        agent.ValidateAndSetModel(null);
        Assert.Equal("auto", agent.Model);
    }

    [Fact]
    public void AgentRecord_ValidateModel_DefaultsEmptyToAuto()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic" };
        agent.ValidateAndSetModel("  ");
        Assert.Equal("auto", agent.Model);
    }

    [Fact]
    public void AgentRecord_ValidateModel_ThrowsForUnknownModel()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic" };
        Assert.Throws<InvalidOperationException>(() => agent.ValidateAndSetModel("gpt-3"));
    }

    [Fact]
    public void AgentRecord_HasPod_ReturnsTrueWhenPodNameSet()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic", PodName = "pod-1" };
        Assert.True(agent.HasPod);
    }

    [Fact]
    public void AgentRecord_HasPod_ReturnsFalseWhenNoPod()
    {
        var agent = new AgentRecord { Name = "test", Provider = "anthropic" };
        Assert.False(agent.HasPod);
    }
}
