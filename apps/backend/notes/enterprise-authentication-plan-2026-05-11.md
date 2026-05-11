# Enterprise Authentication Plan

Date: 2026-05-11

## Decision

GitHub and Google OAuth are not sufficient for enterprise customers. They are useful bootstrap/social login providers, but enterprise buyers expect organization-managed identity: OIDC first, SAML next, SCIM provisioning, enforced domain rules, group-to-role mapping, auditable admin controls, and billing tied to the organization rather than only a user.

The implementation should be owned by the existing Management and Billing features. Do not create a top-level `Auth` feature. This backend's architecture guide explicitly avoids that feature name and keeps organization, members, workspaces, teams, audit, and dashboard auth in `src/Features/Management`.

## Current Backend State

### Login and sessions

- Google and GitHub OAuth are hard-coded in `src/Features/Management/Application/AuthService.cs`.
- REST login/callback endpoints live in `src/Features/Management/Api/AuthController.cs`.
- GraphQL profile/logout endpoints live in `src/Features/Management/Api/AuthMutations.cs` and `src/Features/Management/Api/AuthQueries.cs`.
- OAuth provider config is registered in `src/Program.cs` using `GoogleOAuthConfig` and `GitHubOAuthConfig`.
- Sessions are first-party cookies named `eaos-session`, persisted through `ISessionRepository`.
- `UserEntity` only has `GoogleSubjectId` and `GitHubSubjectId`; there is no generic external identity model.
- `OAuthTokenEntity` stores provider access/refresh tokens for user integrations, not enterprise login configuration.

### Organizations, roles, teams, and invitations

- Organization membership is in `src/Features/Management/Domain/OrgMemberRecord.cs`.
- Organization records are in `src/Features/Management/Domain/OrganizationRecord.cs`.
- Persistence is `src/Database/Models/OrganizationEntity.cs` and `src/Database/Models/OrgMemberEntity.cs`.
- `OrganizationService` currently creates a default organization per owner and supports owner-only email invites.
- Invitations are pending `OrgMemberEntity` rows keyed by email. There is no invitation token, expiry, domain policy, IdP binding, or SCIM source metadata.
- Org roles are `Owner`, `Admin`, `Editor`, and `Viewer`.
- Organization "teams" already exist as access groups:
  - `src/Features/Management/Application/AccessGroupService.cs`
  - `src/Features/Management/Domain/AccessGroupRecord.cs`
  - `src/Database/Models/AccessGroupEntity.cs`
  - `src/Database/Models/AccessGroupMemberEntity.cs`
  - `src/Database/Models/AccessGroupWorkspaceGrantEntity.cs`
- Access groups grant workspace roles and should become the natural target for IdP group mapping.

### Billing and Stripe

- User billing is more complete than org billing:
  - `src/Features/Billing/Application/UserBillingService.cs`
  - `src/Features/Billing/Api/BillingMutations.cs`
  - `src/Features/Billing/Api/BillingQueries.cs`
- Org billing exists but is incomplete:
  - `src/Features/Billing/Application/OrgBillingService.cs`
  - `src/Features/Billing/Domain/OrgSubscriptionRecord.cs`
  - `src/Features/Billing/Infrastructure/OrgSubscriptionRepository.cs`
- Stripe webhook handling currently updates user subscriptions only in `src/Features/Billing/Infrastructure/StripeWebhookService.cs`.
- `GetOrgSubscription` currently accepts an `organizationId` string directly. Enterprise auth work should tighten this by requiring `UserContext` and organization admin/owner authorization.
- Enterprise SSO should be a paid organization capability, likely attached to Team/Enterprise org plans, not individual Pro.

### Audit logging

- Organization audit events already exist in `src/Events/OrganizationAuditSourceEvents.cs`.
- Audit persistence/export exists under `src/Features/Management/Application/OrganizationAuditLogService.cs` and `src/Features/Management/Infrastructure/OrganizationAuditLogRepository.cs`.
- Enterprise auth should add SSO, SCIM, domain, role-mapping, and invite lifecycle audit events instead of logging these only through `ILogger`.

## Skills To Use

Use these local GRC skills from `notes/grc/skills` while implementing and reviewing the feature:

- `notes/grc/skills/gdpr-compliance/SKILL.md`: personal data minimization, lawful basis, retention, data subject rights, breach/security obligations.
- `notes/grc/skills/iso27701/SKILL.md`: privacy management controls, PII controller/processor treatment, RoPA/DPIA implications for identity data.
- `notes/grc/skills/iso27001/SKILL.md`: access control policy, supplier/security controls, evidence expectations, ISMS alignment.
- `notes/grc/skills/soc2/SKILL.md`: SOC 2 CC6 logical access controls, access review evidence, audit logging, policy and evidence requirements.
- `notes/grc/skills/nist-csf/SKILL.md`: CSF 2.0 Govern/Protect/Detect mapping for identity and access management roadmap.
- `notes/grc/skills/cis-controls/SKILL.md`: CIS Controls 5 and 6 for account management, MFA, RBAC, access granting/revocation.
- `notes/grc/skills/nis2/SKILL.md`: EU access control, MFA, incident, governance, and management accountability requirements for in-scope customers.
- `notes/grc/skills/dora/SKILL.md`: use only for EU financial-sector customers or when enterprise auth must satisfy DORA ICT risk and third-party access obligations.

## Target Product Shape

### Enterprise SSO

Add organization-scoped SSO connections:

- OIDC generic connection first.
- SAML 2.0 connection second.
- One or more verified domains per organization.
- Optional `force_sso` policy per organization/domain.
- Optional fallback/break-glass owner account path.
- JIT user creation from trusted IdP claims.
- JIT membership assignment when the domain and IdP connection allow it.
- Group claim mapping to org roles and access groups.
- IdP metadata refresh and key rotation support.
- Audit every config change and every login enforcement decision.

### SCIM provisioning

Add SCIM 2.0 after OIDC foundation:

- Organization-scoped SCIM bearer token.
- `/api/scim/v2/Users` and `/api/scim/v2/Groups`.
- IdP-driven create/update/deactivate for users.
- IdP-driven group membership sync into Access Groups.
- Deactivation removes active sessions and revokes workspace/access-group grants.
- SCIM-managed members should be visibly marked as externally managed in the dashboard.

### Organization teams

Treat Access Groups as the enterprise team model:

- Rename dashboard copy to "Teams" if needed, but keep backend names unless doing a deliberate rename.
- Add optional external group binding fields to access groups.
- IdP group claim or SCIM group should map into access groups.
- Access group workspace grants remain the authorization mechanism for teams.
- Manual edits to externally managed teams should either be blocked or clearly marked as local overrides.

### Roles

Current roles are enough for the first version:

- `Owner`: billing, SSO config, SCIM config, domain verification, transfer ownership.
- `Admin`: member/team/workspace management, maybe SSO read-only depending policy.
- `Editor`: normal workspace contributor.
- `Viewer`: read-only workspace member.

Enterprise auth should add a policy layer around role assignment:

- IdP group mapping can grant `Admin`, `Editor`, or `Viewer`.
- `Owner` should not be assigned automatically by IdP group mapping.
- At least one non-SCIM/non-SSO break-glass owner must remain unless an explicit enterprise override is added.
- Admin/Owner changes require audit events.

### Invitations

The current email-only invite model is too weak for enterprise.

Replace it with a single organization invitation workflow:

- Email invite still exists for non-SSO organizations.
- For SSO-enforced domains, invite acceptance must require login through the organization's IdP.
- Invites need token, expiry, inviter, accepted/revoked timestamps, intended role, and optional workspace/access-group targets.
- Pending invites should be suppressed or auto-resolved when SCIM has already provisioned the user.
- If SCIM is enabled for an org, prefer SCIM provisioning over ad hoc manual invites.

## Proposed Domain Model

Add Management domain records and repositories:

- `OrganizationSsoConnectionRecord`
- `OrganizationSsoConnectionFilter`
- `IOrganizationSsoConnectionRepository`
- `OrganizationDomainRecord`
- `OrganizationDomainFilter`
- `IOrganizationDomainRepository`
- `OrganizationInvitationRecord`
- `OrganizationInvitationFilter`
- `IOrganizationInvitationRepository`
- `ExternalIdentityRecord`
- `ExternalIdentityFilter`
- `IExternalIdentityRepository`
- `ScimTokenRecord`
- `ScimTokenFilter`
- `IScimTokenRepository`
- `ScimSyncStateRecord`

Keep provider implementation details in Infrastructure:

- `OidcIdentityProviderClient`
- `SamlIdentityProviderClient`
- `ScimProvisioningAdapter`
- `SsoCredentialProtector`

Avoid adding public service interfaces inside implementation files. Use dedicated files such as:

- `IEnterpriseAuthenticationService.cs`
- `EnterpriseAuthenticationService.cs`
- `EnterpriseAuthenticationContracts.cs` if request/result records are tightly coupled.

## Proposed Database Changes

Add database entities under `src/Database/Models`:

- `OrganizationSsoConnectionEntity`
- `OrganizationDomainEntity`
- `OrganizationInvitationEntity`
- `ExternalIdentityEntity`
- `ScimTokenEntity`
- `ScimGroupMappingEntity`

Add EF configuration in `src/Database/EaosDbContext.cs`:

- `OrganizationSsoConnection`: unique `(OrganizationId, ProviderKey)`; encrypted client secret/certs; enabled flag.
- `OrganizationDomain`: unique normalized domain; verification status; enforcement policy.
- `OrganizationInvitation`: token hash, email, organization, role, status, expiry, inviter.
- `ExternalIdentity`: unique `(Provider, Issuer, Subject)` and unique `(OrganizationId, UserId, Provider)`.
- `ScimToken`: token hash only, organization id, created/revoked metadata.
- `ScimGroupMapping`: external group id/display name to Access Group and/or org role.

Add migrations. Do not manually edit the model snapshot except as part of migration generation/fixup.

## API Plan

### REST endpoints

Keep browser redirects as REST:

- `GET /api/auth/sso/{organizationSlugOrDomain}`
- `GET /api/auth/sso/callback/{connectionId}`
- `POST /api/scim/v2/Users`
- `GET /api/scim/v2/Users`
- `GET /api/scim/v2/Users/{id}`
- `PUT/PATCH /api/scim/v2/Users/{id}`
- `DELETE /api/scim/v2/Users/{id}`
- Same shape for `/api/scim/v2/Groups`.

### GraphQL

Add Management GraphQL API:

- `OrganizationSsoQueries`
- `OrganizationSsoMutations`
- `OrganizationDomainQueries`
- `OrganizationDomainMutations`
- `OrganizationInvitationMutations`
- `ScimMutations`

Use Api `*Input` and `*Payload` records only. Map to Application request records inside API methods.

Minimum admin operations:

- Create/update/delete OIDC connection.
- Upload/update SAML metadata.
- Verify domain.
- Set domain login policy: allow social, prefer SSO, force SSO.
- Configure group claim mappings.
- Generate/revoke SCIM token.
- View SSO/SCIM audit events.
- Create/revoke invitation.

## Application Flow

### OIDC login

1. Resolve organization by verified domain or explicit route.
2. Load enabled `OrganizationSsoConnectionRecord`.
3. Build authorization URL using discovery metadata and PKCE.
4. Store state/nonce/PKCE verifier in distributed cache.
5. Callback validates state, nonce, issuer, audience, expiry, signature, and email verification.
6. Resolve or create `ExternalIdentityRecord`.
7. Resolve or create `UserRecord`.
8. Apply organization membership:
   - active invite match;
   - SCIM pre-provisioned member;
   - verified-domain JIT policy;
   - otherwise deny.
9. Apply group-to-role and group-to-access-group mapping.
10. Create session and emit audit events.

### SAML login

Implement only after OIDC:

1. Store SP entity ID, ACS URL, IdP metadata, signing/encryption requirements.
2. Validate response signature, issuer, audience, recipient, destination, `NotBefore`/`NotOnOrAfter`.
3. Map NameID and email claims into `ExternalIdentityRecord`.
4. Reuse the same membership/session flow as OIDC.

### SCIM user lifecycle

1. Authenticate SCIM bearer token by hash.
2. Create or update organization member and linked user identity data.
3. If active user exists, maintain `ExternalIdentityRecord`.
4. On deactivate/delete, mark membership inactive, remove access group memberships, revoke sessions, and publish audit events.
5. For SCIM group updates, sync to Access Groups.

## Stripe and Packaging

Enterprise auth should be organization-plan gated:

- Free org: no custom SSO, no SCIM.
- Team org: maybe one OIDC connection, no SAML/SCIM unless product wants SSO in Team.
- Enterprise org: OIDC, SAML, SCIM, multiple domains, audit export, custom role mappings.

Required billing fixes:

- Add org checkout and portal flows to `OrgBillingService`.
- Extend Stripe webhooks to update `OrgSubscriptionEntity`, not only `UserSubscriptionEntity`.
- Add authorization to org subscription queries/mutations.
- Add plan checks in `EnterpriseAuthenticationService` before enabling SSO/SCIM.
- Use Stripe metadata with `type=org` and `organizationId`.

## Compliance And Audit Requirements

Use domain events for all lifecycle facts:

- `OrganizationSsoConnectionCreatedEvent`
- `OrganizationSsoConnectionUpdatedEvent`
- `OrganizationSsoConnectionDeletedEvent`
- `OrganizationDomainVerifiedEvent`
- `OrganizationSsoPolicyUpdatedEvent`
- `OrganizationSsoLoginSucceededEvent`
- `OrganizationSsoLoginFailedEvent`
- `OrganizationScimTokenCreatedEvent`
- `OrganizationScimTokenRevokedEvent`
- `OrganizationScimUserProvisionedEvent`
- `OrganizationScimUserDeactivatedEvent`
- `OrganizationGroupMappingUpdatedEvent`
- `OrganizationInvitationCreatedEvent`
- `OrganizationInvitationRevokedEvent`
- `OrganizationInvitationAcceptedEvent`

Event handlers should stay thin and delegate to the audit log service or repository. Do not put identity business rules in handlers.

Privacy defaults:

- Store only required claims: issuer, subject, email, email verified, display name, avatar if needed.
- Avoid storing raw ID tokens, SAML assertions, or SCIM bearer tokens.
- Store token/certificate secrets encrypted or as hashes, depending whether they must be recovered.
- Add retention policy for failed login events and IdP metadata snapshots.

## Implementation Phases

### Phase 1: Foundation and schema

- Add generic external identity records instead of more `UserEntity` provider columns.
- Add organization domains.
- Add organization SSO connection records.
- Add organization invitations with token hash and expiry.
- Add repositories, EF entities, migrations, and tests.
- Keep Google/GitHub login working during this phase.

### Phase 2: OIDC enterprise SSO

- Add generic OIDC connection setup.
- Implement discovery, JWKS validation, state, nonce, and PKCE.
- Add SSO login and callback endpoints.
- Implement JIT membership and domain enforcement.
- Add group claim mapping to org roles and Access Groups.
- Add audit events and tests.

### Phase 3: Invitation and organization membership cleanup

- Replace email-only invite acceptance with tokenized invitations.
- Enforce SSO for SSO-managed domains.
- Add invite expiry/revocation.
- Add dashboard API payloads for invitation status and source.
- Make owner/admin permissions explicit and consistent across org, access group, and billing operations.

### Phase 4: Stripe organization plans

- Add org checkout and billing portal.
- Update Stripe webhook logic for org subscriptions.
- Gate SSO/SCIM features by org plan.
- Add admin-only billing authorization for organization subscription reads/writes.

### Phase 5: SCIM provisioning

- Add SCIM token generation and revocation.
- Add SCIM Users and Groups endpoints.
- Map SCIM groups into Access Groups.
- Revoke sessions and workspace grants on SCIM deactivation.
- Add provisioning audit events and tests.

### Phase 6: SAML

- Add SAML connection storage and metadata upload.
- Add ACS endpoint and response validation.
- Reuse membership/session/group mapping flow.
- Add SAML-specific tests for signature, audience, recipient, replay, and expiry rejection.

## Test Plan

Add tests under `tests/Management`, `tests/Billing`, and possibly `tests/EnterpriseAuthentication` if the area grows.

Minimum tests:

- OIDC callback rejects invalid state, nonce, issuer, audience, expiry, and unverified email.
- OIDC callback creates/link external identity and session.
- Verified-domain JIT creates org membership only when policy allows it.
- Force-SSO domain blocks Google/GitHub login for that domain.
- Group claim grants expected org role and Access Group membership.
- Owner role cannot be assigned by IdP mapping.
- Tokenized invite accepts only matching email and non-expired token.
- SCIM create/update/deactivate changes members, access groups, and sessions.
- Org subscription gating blocks SSO/SCIM on unsupported plans.
- Stripe webhook updates org subscription records.
- Audit events are emitted for config, login, SCIM, and invite lifecycle.

## Open Product Decisions

- Whether Team plan includes OIDC SSO or only Enterprise.
- Whether social login should be disabled globally once any verified domain is force-SSO.
- Whether Access Groups should be renamed to Teams in backend or only dashboard copy.
- Whether SAML is needed before SCIM for first enterprise customer.
- Whether SCIM deactivation deletes membership or marks it inactive for audit/history.
- Whether enterprise customers can require all members to be SCIM-managed.
