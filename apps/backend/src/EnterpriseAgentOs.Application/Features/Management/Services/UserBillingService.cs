namespace EnterpriseAgentOs.Application.Features.Management;

internal sealed class UserBillingService : IUserBillingService
{
    private readonly StripeConfig _stripeConfig;
    private readonly FrontendConfig _frontendConfig;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly ILogger<UserBillingService> _logger;

    public UserBillingService(
        StripeConfig config,
        FrontendConfig frontend,
        IUserSubscriptionRepository repo,
        ILogger<UserBillingService> logger)
    {
        _stripeConfig = config;
        _frontendConfig = frontend;
        _userSubscriptionRepository = repo;
        _logger = logger;
        StripeConfiguration.ApiKey = _stripeConfig.SecretKey;
    }

    public async Task<UserSubscription> GetSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await _userSubscriptionRepository.GetByAsync(new UserSubscriptionFilter { UserId = userId }, ct);
        return sub ?? UserSubscription.CreateDefaultFree(userId);
    }

    public async Task<CreditBudgetResult> CheckCreditBudgetAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await GetSubscriptionAsync(userId, ct);
        return sub.CheckBudget();
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid userId, string email, string plan, string billingCycle, CancellationToken ct = default)
    {
        var customerId = await GetOrCreateCustomerAsync(userId, email, ct);

        var parsedPlan = plan.ToSubscriptionPlan();
        var parsedCycle = billingCycle.ToBillingCycle();
        var priceId = (parsedPlan, parsedCycle) switch
        {
            (SubscriptionPlan.Pro, BillingCycle.Yearly)  => _stripeConfig.ProYearlyPriceId,
            (SubscriptionPlan.Pro, BillingCycle.Monthly)  => _stripeConfig.ProMonthlyPriceId,
            _                                            => _stripeConfig.FreePriceId,
        };

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            SuccessUrl = $"{_frontendConfig.Origin}/settings/billing?checkout=success",
            CancelUrl = $"{_frontendConfig.Origin}/pricing",
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["plan"] = plan,
                ["billingCycle"] = billingCycle,
            },
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: ct);
        _logger.LogInformation("Created checkout session {SessionId} for user {UserId} plan {Plan}", session.Id, userId, plan);
        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var customerId = await GetOrCreateCustomerAsync(userId, email, ct);
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = $"{_frontendConfig.Origin}/settings/billing",
        };
        var session = await new Stripe.BillingPortal.SessionService().CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    public Task CancelSubscriptionAsync(Guid userId, string email, CancellationToken ct = default)
        => EnableOverageAsync(userId, email, enabled: false, ct);

    public async Task EnableOverageAsync(Guid userId, string email, bool enabled, CancellationToken ct = default)
    {
        var sub = await _userSubscriptionRepository.GetByAsync(new UserSubscriptionFilter { UserId = userId }, ct);
        if (sub is null)
        {
            sub = UserSubscription.CreateDefaultFree(userId);
            await _userSubscriptionRepository.AddAsync(sub, ct);
        }

        if (enabled)
        {
            if (sub.OverageEnabled) return;

            var priceId = GetOveragePriceId(sub.Plan);

            try
            {
                var customerId = await GetOrCreateCustomerAsync(userId, email, ct);

                if (sub.StripeSubscriptionId is not null)
                {
                    var item = await new SubscriptionItemService().CreateAsync(
                        new SubscriptionItemCreateOptions { Subscription = sub.StripeSubscriptionId, Price = priceId },
                        cancellationToken: ct);
                    sub.StripeOverageItemId = item.Id;
                }
                else
                {
                    var newSub = await new SubscriptionService().CreateAsync(
                        new SubscriptionCreateOptions
                        {
                            Customer = customerId,
                            Items = [new SubscriptionItemOptions { Price = priceId }],
                        },
                        cancellationToken: ct);
                    sub.StripeSubscriptionId = newSub.Id;
                    sub.StripeOverageItemId = newSub.Items.Data[0].Id;
                }

                sub.StripeCustomerId = customerId;
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe overage enable failed for user {UserId}", userId);
                throw new BillingProviderException("Billing provider could not enable extra usage. Please try again.", ex);
            }

            sub.OverageEnabled = true;
            _logger.LogInformation("Overage enabled for user {UserId}", userId);
        }
        else
        {
            if (!sub.OverageEnabled) return;

            if (sub.StripeOverageItemId is not null)
            {
                EnsureStripeSecretConfigured();
                try
                {
                    await new SubscriptionItemService().DeleteAsync(
                        sub.StripeOverageItemId,
                        new SubscriptionItemDeleteOptions { ClearUsage = true },
                        cancellationToken: ct);
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex, "Stripe overage disable failed for user {UserId}", userId);
                    throw new BillingProviderException("Billing provider could not disable extra usage. Please try again.", ex);
                }
            }

            sub.StripeOverageItemId = null;
            sub.OverageEnabled = false;
            _logger.LogInformation("Overage disabled for user {UserId}", userId);
        }

        await _userSubscriptionRepository.UpdateAsync(sub, ct);
    }

    private string GetOveragePriceId(SubscriptionPlan plan)
    {
        EnsureStripeSecretConfigured();

        var priceId = plan == SubscriptionPlan.Pro
            ? _stripeConfig.ProOveragePriceId
            : _stripeConfig.FreeOveragePriceId;

        if (string.IsNullOrWhiteSpace(priceId))
        {
            throw new BillingProviderException("Extra usage billing is not configured for this plan.");
        }

        return priceId;
    }

    private void EnsureStripeSecretConfigured()
    {
        if (string.IsNullOrWhiteSpace(_stripeConfig.SecretKey))
        {
            throw new BillingProviderException("Extra usage billing is not configured.");
        }
    }

    public async Task<IReadOnlyList<InvoicePayload>> ListInvoicesAsync(
        Guid userId, CancellationToken ct = default)
    {
        var sub = await _userSubscriptionRepository.GetByAsync(new UserSubscriptionFilter { UserId = userId }, ct);
        if (sub?.StripeCustomerId is null)
        {
            return Array.Empty<InvoicePayload>();
        }
        try
        {
            var invoices = await new InvoiceService().ListAsync(
                new InvoiceListOptions { Customer = sub.StripeCustomerId, Limit = 24 },
                cancellationToken: ct);
            return invoices.Data.Select(i => new InvoicePayload(
                i.Id,
                i.Created,
                ((decimal)i.Total / 100m).ToString("0.00"),
                (i.Currency ?? "eur").ToUpperInvariant(),
                i.Status ?? "unknown",
                i.HostedInvoiceUrl,
                i.InvoicePdf)).ToList();
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe invoice list failed for user {UserId}", userId);
            return Array.Empty<InvoicePayload>();
        }
    }

    public async Task<IReadOnlyDictionary<string, (long MonthlyAmountCents, long YearlyAmountCents, string Currency)>> GetPlanPricesAsync(CancellationToken ct = default)
    {
        var result = new Dictionary<string, (long MonthlyAmountCents, long YearlyAmountCents, string Currency)>();

        if (string.IsNullOrWhiteSpace(_stripeConfig.SecretKey)) return result;

        var priceService = new PriceService();
        var priceIds = new Dictionary<string, (string Monthly, string Yearly)>
        {
            ["pro"] = (_stripeConfig.ProMonthlyPriceId, _stripeConfig.ProYearlyPriceId),
            ["team"] = (_stripeConfig.TeamMonthlyPriceId, _stripeConfig.TeamYearlyPriceId),
        };

        foreach (var (plan, ids) in priceIds)
        {
            try
            {
                long monthly = 0, yearly = 0;
                var currency = "eur";

                if (!string.IsNullOrEmpty(ids.Monthly))
                {
                    var p = await priceService.GetAsync(ids.Monthly, cancellationToken: ct);
                    monthly = p.UnitAmount ?? 0;
                    currency = p.Currency ?? "eur";
                }
                if (!string.IsNullOrEmpty(ids.Yearly))
                {
                    var p = await priceService.GetAsync(ids.Yearly, cancellationToken: ct);
                    yearly = p.UnitAmount ?? 0;
                }

                result[plan] = (monthly, yearly, currency.ToUpperInvariant());
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch Stripe price for plan {Plan}", plan);
            }
        }

        // Free is always 0
        result["free"] = (0, 0, "EUR");

        return result;
    }

    private async Task<string> GetOrCreateCustomerAsync(Guid userId, string email, CancellationToken ct)
    {
        var existing = await _userSubscriptionRepository.GetByAsync(new UserSubscriptionFilter { UserId = userId }, ct);

        if (existing?.StripeCustomerId is not null)
            return existing.StripeCustomerId;

        var customer = await new CustomerService().CreateAsync(
            new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string> { ["type"] = "user", ["userId"] = userId.ToString() },
            },
            cancellationToken: ct);

        _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);

        var sub = await _userSubscriptionRepository.GetByAsync(new UserSubscriptionFilter { UserId = userId }, ct);
        if (sub is null)
        {
            sub = UserSubscription.CreateDefaultFree(userId);
            await _userSubscriptionRepository.AddAsync(sub, ct);
        }
        sub.StripeCustomerId = customer.Id;
        await _userSubscriptionRepository.UpdateAsync(sub, ct);

        return customer.Id;
    }

}
