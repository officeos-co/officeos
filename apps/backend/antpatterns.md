# Anti-patterns — tracking

## Fixed (2026-04-19)
- ~~Application billing services using DbContext directly~~ → now use IUserSubscriptionRepository / IOrgSubscriptionRepository
- ~~Api middleware/controllers querying DbContext~~ → AgentTokenAuthAttribute, AgentAuthInterceptor, AgentBootstrapController all use repository interfaces
- ~~Hardcoded model cost weights in BillingQueries~~ → uses ModelCostWeights.GetWeights() from Domain
- ~~Hardcoded plan descriptions in BillingQueries~~ → uses PlanLimit.Description from Domain
- ~~Tool key splitting logic in AgentMutations~~ → moved to AgentToolPermissionRecord.ParseToolKey()
- ~~Anemic domain model~~ → AgentRecord, UserSubscription, OrgSubscription now have rich methods
- ~~GdprService using DbContext directly~~ → now uses repository interfaces (IUserRepository, IAgentRepository, IAgentLogRepository, ISessionRepository, ISkillRepository)
- ~~ProviderSeeder using DbContext directly~~ → now uses IProviderRepository
- ~~SessionAuthMiddleware hardcoded skip list~~ → externalized to SessionAuthConfig in appsettings.json
- ~~EaosDbContext/Persistence imported in Application GlobalUsings~~ → removed, preventing accidental direct DB access

## Remaining (future refactor)
- Application still references Infrastructure project for config classes (StripeConfig, FrontendConfig, PlatformKeysConfig), protectors (ProviderKeyProtector, SkillCredentialProtector), and adapters (SkillRuntimeClient). To fully decouple: create Domain interfaces for these and implement in Infrastructure.
- AgentTemplateService catches DbUpdateException (EF Core type) — ideally this would be caught in the repository layer instead.
