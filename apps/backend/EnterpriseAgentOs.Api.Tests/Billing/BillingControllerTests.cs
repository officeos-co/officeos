namespace EnterpriseAgentOs.Api.Tests.Billing;

/// <summary>
/// Integration tests for Billing GraphQL (user subscription) + the Stripe webhook
/// REST endpoint, which remains the only REST endpoint in the Billing domain.
/// Stripe is disabled (Enabled=false) in the test factory, so the webhook returns 503
/// and the subscribe mutation surfaces a service-unavailable error.
/// </summary>
public sealed class BillingControllerTests : IClassFixture<EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory>
{
    private readonly EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory _factory;

    public BillingControllerTests(EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory factory) => _factory = factory;

    private const string SubscriptionQuery = @"
        {
          userSubscription {
            plan
            concurrentAgentLimit
            creditBudgetPerMonth
            isActive
          }
        }";

    // -------------------------------------------------------------------------
    // userSubscription GraphQL query
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSubscription_ReturnsFreePlan()
    {
        var client = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLAsync(client, SubscriptionQuery);
        var sub = data.GetProperty("userSubscription");
        Assert.Equal("free", sub.GetProperty("plan").GetString());
    }

    [Fact]
    public async Task GetSubscription_FreePlan_HasCorrectCreditBudget()
    {
        var client = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLAsync(client, SubscriptionQuery);
        Assert.Equal(500_000L, data.GetProperty("userSubscription")
            .GetProperty("creditBudgetPerMonth").GetInt64());
    }

    [Fact]
    public async Task GetSubscription_FreePlan_HasConcurrentAgentLimitOfOne()
    {
        var client = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLAsync(client, SubscriptionQuery);
        Assert.Equal(1, data.GetProperty("userSubscription")
            .GetProperty("concurrentAgentLimit").GetInt32());
    }

    [Fact]
    public async Task GetSubscription_FreePlan_IsActive()
    {
        var client = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLAsync(client, SubscriptionQuery);
        Assert.True(data.GetProperty("userSubscription").GetProperty("isActive").GetBoolean());
    }

    // -------------------------------------------------------------------------
    // subscribeUser mutation — disabled surfaces as a GraphQL error
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Subscribe_WhenBillingDisabled_ReturnsError()
    {
        var client = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        const string mutation = @"
            mutation($plan: String!, $billingCycle: String!) {
              subscribeUser(plan: $plan, billingCycle: $billingCycle) { checkoutUrl }
            }";
        var raw = await EnterpriseAgentOs.Api.Tests.Infrastructure.TestHelpers.GraphQLRawAsync(client, mutation,
            new { plan = "pro", billingCycle = "monthly" });

        // Stripe disabled → service throws, which HotChocolate surfaces as a GraphQL error.
        Assert.True(raw.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);
    }

    // -------------------------------------------------------------------------
    // POST /api/billing/webhook — still REST (Stripe webhook, no auth)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Webhook_WhenBillingDisabled_Returns503()
    {
        var client = _factory.CreateClient();

        var content = new StringContent("{\"type\":\"test\"}", System.Text.Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", "t=1,v1=abc");
        var response = await client.PostAsync("/api/billing/webhook", content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenBillingDisabled_ReturnsErrorBody()
    {
        var client = _factory.CreateClient();

        var content = new StringContent("{\"type\":\"test\"}", System.Text.Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", "t=1,v1=abc");
        var response = await client.PostAsync("/api/billing/webhook", content);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Billing not configured", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Webhook_Endpoint_Exists()
    {
        // The webhook route itself must still be reachable (not 404).
        var client = _factory.CreateClient();

        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/billing/webhook", content);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
