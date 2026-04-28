# EnterpriseAgentOs Backend API Reference

> **Auto-generated** from live schema introspection by `scripts/generate-api-doc.js`.
> Re-generate: `node scripts/generate-api-doc.js`
> Source: `http://localhost:5000`

## Overview

| Interface | Endpoint | Auth | Purpose |
|-----------|----------|------|---------|
| Dashboard GraphQL | `/api/dashboard/graphql` | Session cookie (`eaos-session`) | Operator dashboard |
| Agent GraphQL | `/api/graphql` | Agent bearer token | Agent pod → backend |
| REST | Various `/api/*` paths | Varies (see below) | Webhooks, OAuth, downloads |

---

## Dashboard GraphQL (`/api/dashboard/graphql`)

**Auth:** Session cookie `eaos-session` (set by `GET /api/auth/callback/google`).

### Queries

#### `ping` → `String`


#### `skills` → `[Skill]`
Lists all skills in the catalog with install status, credential status, tools, and metadata.


#### `skill(name: String)` → `Skill`
Returns a single skill by its unique name slug, or null if not found.


#### `skillComments(skillId: UUID)` → `[SkillComment]`
Returns all user comments on a skill, ordered by creation date.


#### `agentSkills(agentId: UUID)` → `[AgentSkillGqlDto]`
Returns skills installed on an agent with their per-tool permission overrides.


#### `oauthConnectionStatus(provider: String, requiredScopes: [String])` → `OAuthConnectionStatus`
Checks whether an OAuth provider is connected and whether any required scopes are missing.


#### `providers` → `[ProviderDto]`
Lists all configured LLM providers with name, display name, and whether an API key is set.


#### `providerModels(providerName: String)` → `[String]`
Returns available model IDs for a specific provider name.


#### `supportedModels` → `[ModelInfoDto]`
Returns all supported models across all providers with display names and default indicator.


#### `org` → `OrganizationPayload`
Returns the authenticated user's organization (auto-created on first call) with member list. Cached for 5 minutes.


#### `agentCronJobs(agentId: UUID)` → `[AgentCronJobRecord]`
Lists all scheduled cron jobs for a specific agent.


#### `channelConnections` → `[ChannelConnectionGqlDto]`
Lists all channel connections (Slack, Telegram, Discord, etc.) configured by the user.


#### `channelConnection(id: UUID)` → `ChannelConnectionGqlDto`
Returns a single channel connection by ID.


#### `channelTypes` → `[ChannelTypeDefinition]`
Returns all supported channel types with display names, descriptions, logos, and onboarding step definitions.


#### `agentChannelBindings(agentId: UUID)` → `[AgentChannelBindingGqlDto]`
Lists all channel bindings for a specific agent showing which channels the agent listens on.


#### `userSubscription` → `UserSubscriptionDto`
Returns the authenticated user's billing subscription including plan, credits, limits, and period.


#### `orgSubscription(organizationId: String)` → `OrgSubscriptionDto`
Returns billing subscription for a specific organization.


#### `planLimits` → `PlanLimitsDto`
Returns the limits for all plan tiers (free, pro, org-free, org-team) including concurrent agents and credits per month.


#### `modelCostWeights` → `[ModelCostWeightDto]`
Returns the credit cost weight multiplier for each supported LLM model.


#### `billing` → `BillingPayload`
Unified billing info for the dashboard billing page. Includes plan, usage, payment method, and invoice history.


#### `tokenUsage(range: String)` → `UserSubscriptionDto`
Returns credit and token usage for the current billing period. Optional range param for historical periods.


#### `me` → `UserPayload`
Returns the authenticated user's profile including email, name, avatar, display name, timezone, and notification preferences. Cached for 2 minutes.


#### `exportMyData` → `GdprExportDto`
GDPR data export. Returns all user data (profile, agents, conversations, audit entries, skill credentials) as a single payload.


#### `agentSessions(agentId: UUID, limit: Int)` → `[AgentSessionRecord]`
Lists conversation sessions for an agent, ordered by most recent. Default limit is 20.


#### `activeSession(agentId: UUID)` → `AgentSessionRecord`
Returns the currently active session for an agent, or null if no session is active.


#### `agentTemplates` → `[AgentTemplateDto]`
Lists all available agent templates (built-in and user-created) for the create-agent-from-template flow.


#### `agents` → `[AgentDto]`
Lists all agents owned by the authenticated user with id, name, provider, model, status, and pod info.


#### `agent(id: UUID)` → `AgentRecord`
Returns a single agent by ID including its full aggregate: personality files, installed skills, memories, channel bindings, and cron jobs.


#### `agentLogs(agentId: UUID, before: DateTime, limit: Int)` → `[AgentLogDto]`
Returns paginated log entries for a specific agent. Supports cursor-based pagination via the before timestamp.


#### `globalLogs(filters: GlobalLogFiltersInput)` → `GlobalLogsPage`
Returns paginated log entries across all agents. Supports filtering by search text, agent name, and log type.


#### `auditLog(agentId: UUID, skip: Int, limit: Int)` → `AuditLogPage`
Returns paginated skill execution audit trail for an agent including tool calls, durations, and results.


### Mutations

#### `noop` → `Boolean`


#### `installSkill(name: String)` → `Boolean`
Installs a skill from the catalog by name. Makes it available for assignment to agents.


#### `uninstallSkill(name: String)` → `Boolean`
Uninstalls a skill by name. Removes it from all agents.


#### `setSkillCredentials(name: String, credentials: [SkillCredentialEntryInput])` → `Boolean`
Sets credential key-value pairs for a skill. Credentials are encrypted at rest.


#### `setSkillRunTarget(name: String, runTarget: String)` → `Boolean`
Sets where a skill executes: cloud (managed) or runner (self-hosted).


#### `likeSkill(skillId: UUID)` → `Skill`
Adds a like from the authenticated user to a skill.


#### `unlikeSkill(skillId: UUID)` → `Skill`
Removes the authenticated user's like from a skill.


#### `commentOnSkill(skillId: UUID, body: String)` → `SkillComment`
Posts a comment on a skill. Body must not be empty.


#### `deleteSkillComment(commentId: UUID)` → `Boolean`
Deletes a skill comment. Only the comment author can delete.


#### `assignSkillToAgent(agentId: UUID, skillName: String)` → `Boolean`
Assigns an installed skill to an agent so it can use the skill's tools.


#### `unassignSkillFromAgent(agentId: UUID, skillName: String)` → `Boolean`
Removes a skill assignment from an agent.


#### `setAgentToolPermission(agentId: UUID, skillName: String, toolName: String, permission: ToolPermission)` → `AgentToolPermissionRecord`
Sets an allow/deny permission override for a specific tool on a specific skill for an agent.


#### `setProviderKey(providerName: String, apiKey: String)` → `ProviderDto`
Sets the API key for an LLM provider. Currently only OpenAI keys are user-configurable.


#### `clearProviderKey(providerName: String)` → `ProviderDto`
Removes the API key for an LLM provider, reverting to the platform default.


#### `trackPageView(input: TrackPageViewInput)` → `Boolean`
Fires a PostHog $pageview event with the given path.


#### `trackNavClicked(input: TrackNavClickedInput)` → `Boolean`
Fires a PostHog nav_clicked event with the navigation destination.


#### `trackSkillInstalled(input: TrackSkillInstalledInput)` → `Boolean`
Fires a PostHog skill_installed event with the skill name.


#### `trackSkillConfigured(input: TrackSkillConfiguredInput)` → `Boolean`
Fires a PostHog skill_configured event with the skill name.


#### `trackChannelConnected(input: TrackChannelConnectedInput)` → `Boolean`
Fires a PostHog channel_connected event with the channel slug.


#### `trackAgentCreated(input: TrackAgentCreatedInput)` → `Boolean`
Fires a PostHog agent_created event with agent name, provider, template, and skill counts.


#### `identifyUser` → `Boolean`
Calls PostHog identify with the authenticated user's email and name.


#### `inviteMember(input: InviteMemberInput)` → `OrgMemberRecord`
Invites a user to the organization by email. Only the org owner can invite.


#### `removeMember(memberId: UUID)` → `Boolean`
Removes a member from the organization. Only the org owner can remove.


#### `renameOrg(input: RenameOrgInput)` → `OrganizationPayload`
Renames the organization. Only the org owner can rename.


#### `createCronJob(input: CreateCronJobInput)` → `AgentCronJobRecord`
Creates a scheduled cron job for an agent. The job sends the specified prompt on the cron schedule.


#### `setCronJobEnabled(id: UUID, enabled: Boolean)` → `Boolean`
Enables or disables a cron job without deleting it.


#### `deleteCronJob(id: UUID)` → `Boolean`
Permanently deletes a cron job.


#### `createChannelConnection(input: CreateChannelConnectionInput)` → `ChannelConnectionGqlDto`
Creates a new channel connection (e.g. Slack bot, Telegram bot). ConfigJson contains the encrypted credentials payload.


#### `updateChannelConnection(id: UUID, input: UpdateChannelConnectionInput)` → `ChannelConnectionGqlDto`
Updates display name and/or enabled status of an existing channel connection.


#### `deleteChannelConnection(id: UUID)` → `Boolean`
Permanently deletes a channel connection and all its agent bindings.


#### `bindChannelToAgent(agentId: UUID, channelConnectionId: UUID, config: ChannelBindingConfigInput)` → `AgentChannelBindingGqlDto`
Binds a channel connection to an agent so it receives messages from that channel. Optional config specifies platform/thread IDs.


#### `unbindChannelFromAgent(agentId: UUID, channelConnectionId: UUID)` → `Boolean`
Removes a channel binding from an agent.


#### `updateChannelBindingConfig(agentId: UUID, channelConnectionId: UUID, config: ChannelBindingConfigInput)` → `AgentChannelBindingGqlDto`
Updates the routing config (platformId, threadId) on an existing agent-channel binding.


#### `subscribeUser(plan: String, billingCycle: String)` → `SubscribeResultDto`
Initiates a Stripe Checkout session for the given plan (free or pro) and billing cycle (monthly or yearly). Returns the checkout URL.


#### `cancelUserSubscription` → `UserSubscriptionDto`
Cancels the user's subscription by disabling overage. Returns the updated subscription state.


#### `setExtraUsageEnabled(enabled: Boolean)` → `Boolean`
Turns extra-usage (Stripe metered overage) on or off. When enabled, usage above the credit budget is billed metered.


#### `updateProfile(input: UpdateProfileInput)` → `UserPayload`
Updates editable profile fields on the authenticated user. Null fields are left unchanged.


#### `logout` → `Boolean`
Clears the current dashboard session cookie. Returns true if a session was deleted.


#### `purgeMyData` → `Boolean`
Permanently deletes all data owned by the authenticated user (GDPR right-to-erasure). Invalidates the current session.


#### `createSession(agentId: UUID)` → `AgentSessionRecord`
Creates a new conversation session for an agent. Ends any active session first and appends a bootstrap system message with personality files.


#### `endSession(agentId: UUID)` → `AgentSessionRecord`
Ends the active session for an agent. Returns the ended session or null if none was active.


#### `createAgentFromTemplate(templateId: UUID, name: String, provider: String, model: String)` → `AgentDto`
Creates a new agent pre-configured from a template (prompt, skills, channels).


#### `createAgent(input: CreateAgentInput)` → `AgentDto`
Creates a new agent with the given config. Optionally assigns skills, tool permissions, and channels.


#### `updateAgent(id: UUID, input: UpdateAgentInput)` → `AgentDto`
Patches mutable fields on an existing agent (name, provider, model, prompt). Null fields are left unchanged.


#### `deleteAgent(id: UUID)` → `Boolean`
Soft-deletes an agent and removes its Kubernetes pod.


#### `sendAgentMessage(agentId: UUID, content: String)` → `AgentLogDto`
Sends a user message to an agent. Creates a MessageIn log entry and triggers the agent turn pipeline.


#### `appendAgentLog(input: AppendAgentLogInput)` → `AgentLogDto`
Appends an arbitrary log entry to an agent's timeline. Used by the dashboard for system events.


### Subscriptions

#### `heartbeat` → `DateTime`


## REST Endpoints

#### `GET /api/auth/google`

#### `GET /api/auth/callback/google`
| Param | Type | Source |
|-------|------|--------|
| `code` | `string` | query |
| `state` | `string` | query |


#### `POST /api/billing/webhook`

#### `GET /api/skills/{name}/bundle`
| Param | Type | Source |
|-------|------|--------|
| `name` | `string` | path |


#### `GET /api/skills/oauth/{provider}/start`
| Param | Type | Source |
|-------|------|--------|
| `scopes` | `string` | query |
| `returnUrl` | `string?` | query |
| `provider` | `string` | path |


#### `GET /api/skills/oauth/{provider}/callback`
| Param | Type | Source |
|-------|------|--------|
| `code` | `string` | query |
| `state` | `string` | query |
| `provider` | `string` | path |


#### `GET /api/skills/oauth/{provider}/status`
| Param | Type | Source |
|-------|------|--------|
| `provider` | `string` | path |


#### `DELETE /api/skills/oauth/{provider}`
| Param | Type | Source |
|-------|------|--------|
| `provider` | `string` | path |


#### `POST channelactive`

#### `POST channelinbound`
| Param | Type | Source |
|-------|------|--------|
| `ChannelType` | `string` | body |
| `SenderIdentifier` | `string` | body |
| `MessageText` | `string` | body |
| `IsGroupMessage` | `bool` | body |
| `MessageId` | `string?` | body |
| `ChannelId` | `string?` | body |


## Types

### Enums

**`AgentLogType`** — `TOOL_CALL | TOOL_RESULT | MESSAGE_IN | MESSAGE_OUT | CHANNEL_IN | CHANNEL_OUT | SYSTEM | AGENT_STARTUP | AGENT_SHUTDOWN | ERROR | ERROR_POD_CONNECTION | ERROR_LLM_CALL | ERROR_TOOL_EXECUTION | ERROR_SKILL_EXECUTION | ERROR_TURN_ORCHESTRATION | ERROR_MEMORY | ERROR_CONFIGURATION`

**`ToolPermission`** — `ALLOW | DENY`


### Object Types

**`AgentChannelBindingGqlDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `channelConnectionId` | `UUID` |  |
| `enabled` | `Boolean` |  |
| `config` | `ChannelBindingConfig` |  |
| `createdAt` | `DateTime` |  |


**`AgentChannelBindingRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `agent` | `AgentRecord` |  |
| `channelConnectionId` | `UUID` |  |
| `channelConnection` | `ChannelConnectionRecord` |  |
| `enabled` | `Boolean` |  |
| `config` | `String` |  |
| `createdAt` | `DateTime` |  |


**`AgentCronJobRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `name` | `String` |  |
| `expression` | `String` |  |
| `prompt` | `String` |  |
| `enabled` | `Boolean` |  |
| `lastRunAt` | `DateTime` |  |
| `nextRunAt` | `DateTime` |  |
| `createdAt` | `DateTime` |  |


**`AgentDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `name` | `String` |  |
| `provider` | `String` |  |
| `model` | `String` |  |
| `prompt` | `String` |  |
| `status` | `String` |  |
| `podName` | `String` |  |
| `serviceUrl` | `String` |  |
| `createdAt` | `DateTime` |  |


**`AgentLogDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `agentName` | `String` |  |
| `time` | `DateTime` |  |
| `type` | `AgentLogType` |  |
| `tool` | `String` |  |
| `integration` | `String` |  |
| `channel` | `String` |  |
| `content` | `String` |  |
| `durationMs` | `Int` |  |
| `inputTokens` | `Int` |  |
| `outputTokens` | `Int` |  |
| `correlationId` | `String` |  |


**`AgentMemoryRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `formatPromptSection` | `String` |  |
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `key` | `String` |  |
| `content` | `String` |  |
| `createdAt` | `DateTime` |  |
| `updatedAt` | `DateTime` |  |
| `agent` | `AgentRecord` |  |


**`AgentPersonalityRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `formatPromptSection` | `String` |  |
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `fileName` | `String` |  |
| `content` | `String` |  |
| `createdAt` | `DateTime` |  |
| `updatedAt` | `DateTime` |  |
| `agent` | `AgentRecord` |  |
| `compositionOrder` | `Int` |  |


**`AgentRateLimitRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `bucketKey` | `String` |  |
| `windowStart` | `DateTime` |  |
| `count` | `Int` |  |


**`AgentRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `name` | `String` |  |
| `provider` | `String` |  |
| `model` | `String` |  |
| `status` | `String` |  |
| `podName` | `String` |  |
| `serviceUrl` | `String` |  |
| `prompt` | `String` |  |
| `createdAt` | `DateTime` |  |
| `isDeleted` | `Boolean` |  |
| `ownerId` | `UUID` |  |
| `encryptedBackendToken` | `String` |  |
| `personalityFiles` | `[AgentPersonalityRecord]` |  |
| `installedSkills` | `[AgentSkillRecord]` |  |
| `memories` | `[AgentMemoryRecord]` |  |
| `cronJobs` | `[AgentCronJobRecord]` |  |
| `rateLimits` | `[AgentRateLimitRecord]` |  |
| `channelBindings` | `[AgentChannelBindingRecord]` |  |
| `skillDetails` | `[SkillRecord]` |  |
| `activeSession` | `AgentSessionRecord` |  |
| `hasPod` | `Boolean` |  |


**`AgentSessionRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `isExpired` | `Boolean` |  |
| `formatBootstrapMessage` | `String` |  |
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `status` | `String` |  |
| `messageCount` | `Int` |  |
| `lastActivityAt` | `DateTime` |  |
| `createdAt` | `DateTime` |  |
| `endedAt` | `DateTime` |  |
| `agent` | `AgentRecord` |  |
| `isActive` | `Boolean` |  |


**`AgentSkillGqlDto`**
| Field | Type | Description |
|-------|------|-------------|
| `skillName` | `String` |  |
| `permissions` | `[AgentToolPermissionRecord]` |  |


**`AgentSkillRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `skillName` | `String` |  |
| `enabledAt` | `DateTime` |  |


**`AgentTemplateDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `name` | `String` |  |
| `description` | `String` |  |
| `prompt` | `String` |  |
| `integrations` | `[String]` |  |
| `channels` | `[String]` |  |
| `isBuiltin` | `Boolean` |  |


**`AgentToolPermissionRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `agent` | `AgentRecord` |  |
| `skillName` | `String` |  |
| `toolName` | `String` |  |
| `permission` | `ToolPermission` |  |
| `createdAt` | `DateTime` |  |
| `updatedAt` | `DateTime` |  |


**`AuditEntry`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `userId` | `UUID` |  |
| `skillName` | `String` |  |
| `action` | `String` |  |
| `paramsJson` | `String` |  |
| `resultSummary` | `String` |  |
| `durationMs` | `Long` |  |
| `timestamp` | `DateTime` |  |


**`AuditLogPage`**
| Field | Type | Description |
|-------|------|-------------|
| `items` | `[AuditEntry]` |  |
| `total` | `Int` |  |


**`BillingPayload`**
| Field | Type | Description |
|-------|------|-------------|
| `plan` | `String` |  |
| `planDescription` | `String` |  |
| `status` | `String` |  |
| `billingCycle` | `String` |  |
| `periodStart` | `DateTime` |  |
| `periodEnd` | `DateTime` |  |
| `creditBudgetPerMonth` | `Long` |  |
| `creditsUsedThisMonth` | `Long` |  |
| `creditsRemaining` | `Long` |  |
| `overBudget` | `Boolean` |  |
| `extraUsageEnabled` | `Boolean` |  |
| `paymentBrand` | `String` |  |
| `paymentLast4` | `String` |  |
| `invoices` | `[InvoicePayload]` |  |


**`ChannelBindingConfig`**
| Field | Type | Description |
|-------|------|-------------|
| `platformId` | `String` |  |
| `threadId` | `String` |  |


**`ChannelConnectionGqlDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `channelType` | `String` |  |
| `displayName` | `String` |  |
| `enabled` | `Boolean` |  |
| `createdAt` | `DateTime` |  |


**`ChannelConnectionRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `channelType` | `String` |  |
| `displayName` | `String` |  |
| `enabled` | `Boolean` |  |
| `createdAt` | `DateTime` |  |
| `createdById` | `UUID` |  |
| `encryptedCreds` | `String` |  |
| `createdBy` | `UserRecord` |  |
| `bindings` | `[AgentChannelBindingRecord]` |  |


**`ChannelTypeDefinition`**
| Field | Type | Description |
|-------|------|-------------|
| `type` | `String` |  |
| `displayName` | `String` |  |
| `description` | `String` |  |
| `logo` | `String` |  |
| `onboardingSteps` | `[OnboardingStep]` |  |


**`CreditBudgetResult`**
| Field | Type | Description |
|-------|------|-------------|
| `remaining` | `Long` |  |
| `overBudget` | `Boolean` |  |


**`GdprAgentDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `name` | `String` |  |
| `provider` | `String` |  |
| `model` | `String` |  |
| `status` | `String` |  |
| `createdAt` | `DateTime` |  |


**`GdprAuditEntryDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `skillName` | `String` |  |
| `action` | `String` |  |
| `paramsJson` | `String` |  |
| `resultSummary` | `String` |  |
| `durationMs` | `Long` |  |
| `timestamp` | `DateTime` |  |


**`GdprConversationDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `agentId` | `UUID` |  |
| `role` | `String` |  |
| `content` | `String` |  |
| `sessionId` | `String` |  |
| `createdAt` | `DateTime` |  |


**`GdprExportDto`**
| Field | Type | Description |
|-------|------|-------------|
| `user` | `GdprUserDto` |  |
| `agents` | `[GdprAgentDto]` |  |
| `conversations` | `[GdprConversationDto]` |  |
| `auditEntries` | `[GdprAuditEntryDto]` |  |
| `skillCredentials` | `[GdprSkillCredentialDto]` |  |


**`GdprSkillCredentialDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `skillName` | `String` |  |
| `enabled` | `Boolean` |  |
| `configuredAt` | `DateTime` |  |


**`GdprUserDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `email` | `String` |  |
| `name` | `String` |  |
| `createdAt` | `DateTime` |  |
| `lastLoginAt` | `DateTime` |  |


**`GlobalLogsPage`**
| Field | Type | Description |
|-------|------|-------------|
| `items` | `[AgentLogDto]` |  |
| `total` | `Int` |  |


**`InvoicePayload`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `String` |  |
| `date` | `DateTime` |  |
| `total` | `String` |  |
| `currency` | `String` |  |
| `status` | `String` |  |
| `hostedUrl` | `String` |  |
| `pdfUrl` | `String` |  |


**`ModelCostWeightDto`**
| Field | Type | Description |
|-------|------|-------------|
| `model` | `String` |  |
| `weight` | `Int` |  |


**`ModelInfoDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `String` |  |
| `displayName` | `String` |  |
| `isDefault` | `Boolean` |  |


**`OAuth2FieldConfig`**
| Field | Type | Description |
|-------|------|-------------|
| `provider` | `String` |  |
| `scopes` | `[String]` |  |


**`OnboardingStep`**
| Field | Type | Description |
|-------|------|-------------|
| `type` | `String` |  |
| `title` | `String` |  |
| `description` | `String` |  |
| `value` | `String` |  |
| `inputKey` | `String` |  |
| `inputLabel` | `String` |  |
| `inputPlaceholder` | `String` |  |
| `inputHelp` | `String` |  |
| `inputKind` | `String` |  |
| `inputRequired` | `Boolean` |  |


**`OrganizationPayload`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `name` | `String` |  |
| `ownerUserId` | `UUID` |  |
| `createdAt` | `DateTime` |  |
| `members` | `[OrgMemberRecord]` |  |


**`OrgMemberRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `organizationId` | `UUID` |  |
| `userId` | `UUID` |  |
| `email` | `String` |  |
| `role` | `String` |  |
| `status` | `String` |  |
| `createdAt` | `DateTime` |  |


**`OrgSubscriptionDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `organizationId` | `String` |  |
| `plan` | `String` |  |
| `concurrentAgentLimit` | `Int` |  |
| `creditBudgetPerMonth` | `Long` |  |
| `creditsUsedThisMonth` | `Long` |  |
| `creditsRemaining` | `Long` |  |
| `overBudget` | `Boolean` |  |
| `overageEnabled` | `Boolean` |  |
| `periodStart` | `DateTime` |  |
| `periodEnd` | `DateTime` |  |
| `isActive` | `Boolean` |  |


**`PlanLimit`**
| Field | Type | Description |
|-------|------|-------------|
| `plan` | `String` |  |
| `concurrentAgents` | `Int` |  |
| `creditsPerMonth` | `Long` |  |
| `description` | `String` |  |


**`PlanLimitsDto`**
| Field | Type | Description |
|-------|------|-------------|
| `individualFree` | `PlanLimit` |  |
| `individualPro` | `PlanLimit` |  |
| `orgFree` | `PlanLimit` |  |
| `orgTeam` | `PlanLimit` |  |


**`ProviderDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `name` | `String` |  |
| `displayName` | `String` |  |
| `configured` | `Boolean` |  |
| `configuredAt` | `DateTime` |  |


**`Skill`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `name` | `String` |  |
| `title` | `String` |  |
| `description` | `String` |  |
| `doc` | `String` |  |
| `status` | `String` |  |
| `version` | `String` |  |
| `requiresApproval` | `Boolean` |  |
| `createdAt` | `DateTime` |  |
| `updatedAt` | `DateTime` |  |
| `logo` | `String` |  |
| `license` | `String` |  |
| `repository` | `String` |  |
| `categories` | `[String]` |  |
| `keywords` | `[String]` |  |
| `readme` | `String` |  |
| `changelog` | `String` |  |
| `likes` | `Int` |  |
| `likedByMe` | `Boolean` |  |
| `commentsCount` | `Int` |  |
| `installed` | `Boolean` |  |
| `configured` | `Boolean` |  |
| `credentialFields` | `[SkillCredentialField]` |  |
| `sourceCodeUrl` | `String` |  |
| `tools` | `[SkillTool]` |  |
| `author` | `SkillAuthor` |  |
| `contributors` | `[SkillContributor]` |  |


**`SkillCredentialRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `skillName` | `String` |  |
| `enabled` | `Boolean` |  |
| `encryptedCredentials` | `String` |  |
| `configuredAt` | `DateTime` |  |
| `runTarget` | `String` |  |


**`SkillOAuth2Config`**
| Field | Type | Description |
|-------|------|-------------|
| `provider` | `String` |  |
| `scopes` | `[String]` |  |


**`SkillRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `actions` | `[KeyValuePairOfStringAndRuntimeActionManifest]` |  |
| `credentialFields` | `[RuntimeCredentialField]` |  |
| `contributors` | `[ManifestContributor]` |  |
| `formatPromptSection` | `String` |  |
| `id` | `UUID` |  |
| `name` | `String` |  |
| `title` | `String` |  |
| `description` | `String` |  |
| `doc` | `String` |  |
| `source` | `String` |  |
| `logo` | `String` |  |
| `license` | `String` |  |
| `repository` | `String` |  |
| `requiresApproval` | `Boolean` |  |
| `readme` | `String` |  |
| `changelog` | `String` |  |
| `category` | `String` |  |
| `authorName` | `String` |  |
| `authorUrl` | `String` |  |
| `categories` | `[String]` |  |
| `keywords` | `[String]` |  |
| `actionsJson` | `String` |  |
| `credentialFieldsJson` | `String` |  |
| `contributorsJson` | `String` |  |
| `bundleS3Key` | `String` |  |
| `version` | `String` |  |
| `status` | `String` |  |
| `buildError` | `String` |  |
| `gitHubRepoUrl` | `String` |  |
| `gitHubBranch` | `String` |  |
| `isSystem` | `Boolean` |  |
| `ownerId` | `UUID` |  |
| `createdAt` | `DateTime` |  |
| `updatedAt` | `DateTime` |  |
| `owner` | `UserRecord` |  |
| `credential` | `SkillCredentialRecord` |  |
| `isActive` | `Boolean` |  |
| `hasDoc` | `Boolean` |  |


**`SubscribeResultDto`**
| Field | Type | Description |
|-------|------|-------------|
| `checkoutUrl` | `String` |  |


**`UserPayload`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `email` | `String` |  |
| `name` | `String` |  |
| `avatarUrl` | `String` |  |
| `displayName` | `String` |  |
| `timezone` | `String` |  |
| `notificationPrefsJson` | `String` |  |
| `preferences` | `String` |  |


**`UserRecord`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `email` | `String` |  |
| `name` | `String` |  |
| `avatarUrl` | `String` |  |
| `googleSubjectId` | `String` |  |
| `createdAt` | `DateTime` |  |
| `lastLoginAt` | `DateTime` |  |
| `displayName` | `String` |  |
| `timezone` | `String` |  |
| `notificationPrefsJson` | `String` |  |
| `preferences` | `String` |  |
| `subscription` | `UserSubscription` |  |


**`UserSubscriptionDto`**
| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` |  |
| `userId` | `UUID` |  |
| `plan` | `String` |  |
| `billingCycle` | `String` |  |
| `concurrentAgentLimit` | `Int` |  |
| `creditBudgetPerMonth` | `Long` |  |
| `creditsUsedThisMonth` | `Long` |  |
| `creditsRemaining` | `Long` |  |
| `overBudget` | `Boolean` |  |
| `overageEnabled` | `Boolean` |  |
| `periodStart` | `DateTime` |  |
| `periodEnd` | `DateTime` |  |
| `isActive` | `Boolean` |  |



### Input Types

**`AppendAgentLogInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `agentId` | `UUID` |  |  |
| `type` | `AgentLogType` |  |  |
| `content` | `String` |  |  |
| `tool` | `String` |  |  |
| `integration` | `String` |  |  |
| `channel` | `String` |  |  |
| `correlationId` | `String` |  |  |


**`ChannelBindingConfigInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `platformId` | `String` |  |  |
| `threadId` | `String` |  |  |


**`CreateAgentInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | `String` |  |  |
| `provider` | `String` |  |  |
| `model` | `String` |  |  |
| `prompt` | `String` |  |  |
| `integrationSlugs` | `[String]` |  |  |
| `channelSlugs` | `[String]` |  |  |
| `toolNames` | `[String]` |  |  |
| `toolPermissions` | `[ToolPermissionInput]` |  |  |
| `bootstrapMessage` | `String` |  |  |


**`CreateChannelConnectionInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `channelType` | `String` |  |  |
| `displayName` | `String` |  |  |
| `configJson` | `String` |  |  |
| `defaultChannelId` | `String` |  |  |


**`CreateCronJobInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `agentId` | `UUID` |  |  |
| `name` | `String` |  |  |
| `expression` | `String` |  |  |
| `prompt` | `String` |  |  |


**`GlobalLogFiltersInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `search` | `String` |  |  |
| `agentName` | `String` |  |  |
| `type` | `AgentLogType` |  |  |
| `skip` | `Int` | `0` |  |
| `limit` | `Int` | `50` |  |


**`InviteMemberInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `email` | `String` |  |  |
| `role` | `String` |  |  |


**`RenameOrgInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | `String` |  |  |


**`SkillCredentialEntryInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `key` | `String` |  |  |
| `value` | `String` |  |  |


**`ToolPermissionInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `tool` | `String` |  |  |
| `mode` | `ToolPermission` |  |  |


**`TrackAgentCreatedInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `agentName` | `String` |  |  |
| `provider` | `String` |  |  |
| `template` | `String` |  |  |
| `skillCount` | `Int` |  |  |
| `allowSkills` | `Int` |  |  |
| `denySkills` | `Int` |  |  |


**`TrackChannelConnectedInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `channelSlug` | `String` |  |  |


**`TrackNavClickedInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `destination` | `String` |  |  |


**`TrackPageViewInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `path` | `String` |  |  |


**`TrackSkillConfiguredInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `skillName` | `String` |  |  |


**`TrackSkillInstalledInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `skillName` | `String` |  |  |


**`UpdateAgentInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | `String` |  |  |
| `provider` | `String` |  |  |
| `model` | `String` |  |  |
| `prompt` | `String` |  |  |


**`UpdateChannelConnectionInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `displayName` | `String` |  |  |
| `configJson` | `String` |  |  |
| `enabled` | `Boolean` |  |  |


**`UpdateProfileInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | `String` |  |  |
| `displayName` | `String` |  |  |
| `timezone` | `String` |  |  |
| `notificationPrefsJson` | `String` |  |  |
| `preferences` | `String` |  |  |


**`UserSubscriptionInput`**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `id` | `UUID` |  |  |
| `userId` | `UUID` |  |  |
| `plan` | `String` |  |  |
| `billingCycle` | `String` |  |  |
| `stripeCustomerId` | `String` |  |  |
| `stripeSubscriptionId` | `String` |  |  |
| `stripeOverageItemId` | `String` |  |  |
| `concurrentAgentLimit` | `Int` |  |  |
| `creditBudgetPerMonth` | `Long` |  |  |
| `creditsUsedThisMonth` | `Long` |  |  |
| `periodStart` | `DateTime` |  |  |
| `periodEnd` | `DateTime` |  |  |
| `isActive` | `Boolean` |  |  |
| `overageEnabled` | `Boolean` |  |  |

